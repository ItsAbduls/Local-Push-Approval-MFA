using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("https://localhost:7244") };
// adjust if different port

Console.WriteLine("📱 MFA Console Simulator Started...");
Console.WriteLine("Supports Push + TOTP fallback");

// Choose mode
Console.WriteLine("\nSelect Mode: (1) Push Approval  (2) TOTP Verification");
var mode = Console.ReadLine();

if (mode == "2")
{
    // === TOTP Flow ===
    Console.Write("Enter user email: ");
    var email = Console.ReadLine();

    while (true)
    {
        Console.Write("Enter TOTP code from authenticator: ");
        var code = Console.ReadLine();

        var response = await client.PostAsJsonAsync("/api/verify-totp", new { email, code });
        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Server Response: {result}");
        Console.WriteLine("Try again or press Ctrl+C to exit.\n");
    }
}
else
{
    // === Push Flow ===
    Console.WriteLine("Polling for Push challenges... (Ctrl+C to exit)");

    while (true)
    {
        try
        {
            var challenges = await client.GetFromJsonAsync<List<ChallengeDto>>("/api/pending-challenges");

            if (challenges is { Count: > 0 })
            {
                Console.WriteLine("\n=== Pending Challenges ===");
                foreach (var ch in challenges)
                {
                    Console.WriteLine($"ID: {ch.Id} | User: {ch.UserId} | Method: {ch.Method}");
                }

                Console.Write("Enter Challenge ID to approve (or press Enter to skip): ");
                var input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input) && Guid.TryParse(input, out var challengeId))
                {
                    var response = await client.PostAsJsonAsync("/api/approve", new { challengeId });
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"✅ {result}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error: {ex.Message}");
        }

        await Task.Delay(5000); // poll every 5 sec
    }
}

record ChallengeDto(Guid Id, Guid UserId, string Method);
