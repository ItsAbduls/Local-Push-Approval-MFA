using MFAApi.Data;
using MFAApi.Models;
using MFAApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Services --------------------

// Add EF Core with SQL Server
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<ChallengeService>();
builder.Services.AddScoped<TotpService>();

// Add SignalR
builder.Services.AddSignalR();

// CORS
// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 🔑 Important for SignalR
    });
});

// JWT Authentication
var key = Encoding.UTF8.GetBytes("SuperSecretKey123!ChangeThis");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MFA API",
        Version = "v1",
        Description = "Multi-Factor Authentication API using JWT, TOTP, and Push challenges"
    });

    // JWT in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token}'"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// -------------------- Middleware --------------------

app.UseCors("AllowAngular");

app.UseAuthentication(); // <-- JWT middleware
app.UseAuthorization();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MFA API v1");
    options.RoutePrefix = "swagger";
});

// Ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// -------------------- API Endpoints --------------------

// Public endpoints (no JWT required)
app.MapPost("/api/register", async (AppDbContext db, string email, string password) =>
{
    if (await db.Users.AnyAsync(u => u.Email == email))
        return Results.BadRequest("User already exists");

    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("Registered");
})
    .AllowAnonymous()
    .WithName("RegisterUser").WithTags("Authentication");

// Login endpoint → broadcast challenge
app.MapPost("/api/login", async (
    AppDbContext db,
    ChallengeService challenges,
    IHubContext<AuthHub> hub,
    [FromBody] LoginRequest request) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    var challenge = await challenges.CreateChallengeAsync(user.Id, "Push");

    // broadcast via SignalR
    await hub.Clients.All.SendAsync("ReceiveChallenge", new
    {
        challenge.Id,
        challenge.UserId,
        challenge.Method
    });

    return Results.Ok(new { challengeId = challenge.Id, method = challenge.Method });
})
    .AllowAnonymous()
    .WithName("Login")
    .WithTags("Authentication");



// Approve endpoint → broadcast approval
app.MapPost("/api/approve", async (ChallengeService challenges, IHubContext<AuthHub> hub, ApproveRequest request) =>
{
    var success = await challenges.MarkApproved(request.ChallengeId);
    if (success)
    {
        await hub.Clients.All.SendAsync("ChallengeApproved", request.ChallengeId);
        return Results.Ok("Approved ✅");
    }
    return Results.BadRequest("Invalid challenge");
})
    .AllowAnonymous()
    .WithName("ApproveChallenge").WithTags("Authentication");

app.MapGet("/api/pending-challenges", async (AppDbContext db) =>
{
    var pending = await db.LoginChallenges
        .Where(c => c.Status == "Pending")
        .Select(c => new { c.Id, c.UserId, c.Method })
        .ToListAsync();
    return Results.Ok(pending);
})
    .AllowAnonymous()
    .WithName("PendingChallenges").WithTags("Admin");

// Protected endpoints (JWT will be required later if you add RequireAuthorization)
app.MapPost("/api/exchange", async (
    AppDbContext db,
    ChallengeService challenges,
    JwtService jwt,
    [FromBody] ExchangeRequest request) =>
{
    var ch = await db.LoginChallenges.FindAsync(request.ChallengeId);
    if (ch is null || ch.Status != "Approved")
        return Results.BadRequest("Not approved");

    var user = await db.Users.FindAsync(ch.UserId);
    if (user is null) return Results.BadRequest();

    var token = jwt.Generate(user);
    await challenges.MarkConsumed(request.ChallengeId);

    return Results.Ok(new { token });
})
.AllowAnonymous()
.WithName("ExchangeChallenge")
.WithTags("Authentication");


app.MapPost("/api/verify-totp", async (AppDbContext db, TotpService totp, JwtService jwt, string email, string code) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user == null) return Results.BadRequest("User not found");

    if (!totp.VerifyCode(code)) return Results.BadRequest("Invalid TOTP code");

    var token = jwt.Generate(user);
    return Results.Ok(new { token });
})
    .AllowAnonymous()
    .WithName("VerifyTotp").WithTags("Authentication");

// SignalR hub
app.MapHub<AuthHub>("/hubs/auth");

app.Run();

// -------------------- SignalR Hub --------------------
class AuthHub : Hub { }
