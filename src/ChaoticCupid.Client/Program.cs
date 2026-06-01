using ChaoticCupid.Shared;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChaoticCupid.Client;

internal static class Program
{
    private const string DefaultHubUrl = "http://localhost:5188/cupid";

    // Set while a delivered letter is waiting for the user to press Enter.
    private static volatile bool _awaitingAcknowledgment;

    private static async Task<int> Main(string[] args)
    {
        var hubUrl = args.Length > 0 ? args[0] : DefaultHubUrl;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        connection.On<LoveLetter>("ReceiveLetter", letter =>
        {
            RenderLetter(letter);
            _awaitingAcknowledgment = true;
        });

        Console.WriteLine("=== Chaotic Cupid ===");
        Console.WriteLine($"Connecting to {hubUrl} ...");

        try
        {
            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not reach the Cupid server: {ex.Message}");
            Console.WriteLine("Make sure the server is running, then try again.");
            return 1;
        }

        Console.WriteLine("Connected!\n");

        if (!await RegisterAsync(connection))
        {
            await connection.DisposeAsync();
            return 1;
        }

        await RunCommandLoopAsync(connection);
        await connection.DisposeAsync();
        return 0;
    }

    private static async Task<bool> RegisterAsync(HubConnection connection)
    {
        Console.WriteLine("Let's get you registered for matchmaking.\n");

        while (true)
        {
            var person = new PersonInfo
            {
                Username = PromptText("Username"),
                City = PromptText("City"),
                Age = PromptNonNegativeInt("Age"),
                Phone = PromptPhone("Phone number"),
            };

            try
            {
                await connection.InvokeAsync("InitSinglePerson", person);
                Console.WriteLine($"\nWelcome, {person.Username}! Cupid is now watching over you.");
                Console.WriteLine("Type \"/block <username>\" to block a sender, or press Enter to acknowledge a letter.\n");
                return true;
            }
            catch (Exception ex)
            {
                // e.g. duplicate username; let the user try again.
                Console.WriteLine($"\nRegistration failed: {ex.Message}\nPlease try again.\n");
            }
        }
    }

    private static async Task RunCommandLoopAsync(HubConnection connection)
    {
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null)
            {
                break; // EOF / Ctrl+D
            }

            line = line.Trim();

            if (line.StartsWith("/block ", StringComparison.OrdinalIgnoreCase))
            {
                var target = line[7..].Trim();
                if (string.IsNullOrWhiteSpace(target))
                {
                    Console.WriteLine("Usage: /block <username>");
                    continue;
                }

                await connection.InvokeAsync("BlockUser", target);
                Console.WriteLine($"You will no longer receive letters from '{target}'.");
                continue;
            }

            if (string.Equals(line, "/quit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (_awaitingAcknowledgment)
            {
                await connection.InvokeAsync("AcknowledgeLetter");
                _awaitingAcknowledgment = false;
                Console.WriteLine("Letter acknowledged. Waiting for the next one...\n");
            }
        }
    }

    private static void RenderLetter(LoveLetter letter)
    {
        Console.WriteLine();
        Console.WriteLine("====== A love letter arrives! ======");
        Console.WriteLine($"  From:  {letter.SenderUsername}");
        Console.WriteLine($"  City:  {letter.SenderCity}");
        Console.WriteLine($"  Age:   {letter.SenderAge}");
        if (letter.SenderPhone is not null)
        {
            Console.WriteLine($"  Phone: {letter.SenderPhone}");
        }

        Console.WriteLine($"  \"{letter.Message}\"");
        Console.WriteLine("====================================");
        Console.WriteLine("Press Enter to acknowledge (no new letters until you do).");
    }

    private static string PromptText(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input))
            {
                return input.Trim();
            }

            Console.WriteLine($"  {label} cannot be empty. Please try again.");
        }
    }

    private static int PromptNonNegativeInt(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine($"  {label} cannot be empty. Please enter a number.");
                continue;
            }

            if (!int.TryParse(input.Trim(), out var value))
            {
                Console.WriteLine($"  {label} must be a number (digits only).");
                continue;
            }

            if (value < 0)
            {
                Console.WriteLine($"  {label} cannot be negative.");
                continue;
            }

            return value;
        }
    }

    private static string PromptPhone(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine($"  {label} cannot be empty. Please enter digits only.");
                continue;
            }

            var trimmed = input.Trim();
            if (!trimmed.All(char.IsDigit))
            {
                Console.WriteLine($"  {label} must contain digits only (no letters or symbols).");
                continue;
            }

            return trimmed;
        }
    }
}
