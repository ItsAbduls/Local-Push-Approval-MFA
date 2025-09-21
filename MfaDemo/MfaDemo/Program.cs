using MfaDemo;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;

Console.WriteLine("📱 MFA Console Simulator with SignalR Started...");

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:7244/hubs/auth") // your API URL
    .WithAutomaticReconnect()
    .Build();

// Event: new challenge received
connection.On<ChallengeDto>("ReceiveChallenge", async (challenge) =>
{
    Console.WriteLine($"\n🚨 New login attempt: ID={challenge.Id}, User={challenge.UserId}, Method={challenge.Method}");
    Console.Write("Approve? (y/n): ");
    var key = Console.ReadKey();
    Console.WriteLine();

    if (key.KeyChar == 'y')
    {
        var client = new HttpClient { BaseAddress = new Uri("https://localhost:7244") };
        await client.PostAsJsonAsync("/api/approve", new { challengeId = challenge.Id });
        Console.WriteLine("✅ Approved!");
    }
});

// Event: challenge approved
connection.On<Guid>("ChallengeApproved", (id) =>
{
    Console.WriteLine($"✅ Challenge {id} approved!");
});

await connection.StartAsync();
Console.WriteLine("Connected to SignalR hub. Waiting for challenges...");
await Task.Delay(-1); // keep console running
