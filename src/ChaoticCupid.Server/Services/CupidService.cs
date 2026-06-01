using ChaoticCupid.Server.Contracts;
using ChaoticCupid.Server.Helpers;
using ChaoticCupid.Server.Hubs;
using ChaoticCupid.Server.State;
using ChaoticCupid.Shared;
using Microsoft.AspNetCore.SignalR;

namespace ChaoticCupid.Server.Services;

/// <summary>
/// The matchmaking publisher. On a fixed interval it scores every eligible
/// candidate for each recipient and pushes the best match's love letter.
/// All scoring/dispatch logic is contained here, away from the data models.
/// </summary>
public sealed class CupidService : BackgroundService, ICupidService
{
    private const int LetterIntervalSeconds = 60;

    private const int SameCityBonus = 30;
    private const int SimilarAgeBonus = 20;
    private const int SimilarAgeThreshold = 2;
    private const int RandomFactorMinInclusive = 0;
    private const int RandomFactorMaxExclusive = 101; // range [0, 100]

    private static readonly string[] Messages =
    {
        "I look forward to our meeting!",
        "I would like to get to know you.",
        "I am not interested in getting acquainted.",
    };

    // Index of the message after which the sender's phone number must be hidden.
    private const int NotInterestedMessageIndex = 2;

    private readonly IHubContext<CupidHub, ILetterClient> _hub;
    private readonly PersonRegistry _registry;
    private readonly ILogger<CupidService> _logger;

    public CupidService(
        IHubContext<CupidHub, ILetterClient> hub,
        PersonRegistry registry,
        ILogger<CupidService> logger)
    {
        _hub = hub;
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(LetterIntervalSeconds));
        _logger.LogInformation("Cupid is awake. Letters dispatch every {Interval}s.", LetterIntervalSeconds);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DispatchLetters(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    public async Task DispatchLetters(CancellationToken cancellationToken)
    {
        var people = _registry.Snapshot();
        if (people.Count < 2)
        {
            return;
        }

        foreach (var recipient in people)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Respect the acknowledgment gate: no new letter while one is pending.
            if (recipient.HasPendingLetter)
            {
                continue;
            }

            var sender = SelectBestCandidate(recipient, people);
            if (sender is null)
            {
                continue;
            }

            // Atomically claim the delivery slot; bail if another round just did.
            if (!recipient.TryBeginDelivery())
            {
                continue;
            }

            var letter = BuildLetter(sender.Info);

            try
            {
                await _hub.Clients.Client(recipient.ConnectionId).ReceiveLetter(letter);
                _logger.LogInformation(
                    "Letter dispatched from {Sender} to {Recipient}.", sender.Username, recipient.Username);
            }
            catch (Exception ex)
            {
                // Delivery failed; release the gate so the next round can retry.
                recipient.AcknowledgeLetter();
                _logger.LogWarning(ex, "Failed to deliver letter to {Recipient}.", recipient.Username);
            }
        }
    }

    private static RegisteredPerson? SelectBestCandidate(
        RegisteredPerson recipient, IReadOnlyList<RegisteredPerson> people)
    {
        RegisteredPerson? best = null;
        var bestScore = int.MinValue;

        foreach (var candidate in people)
        {
            // No self-letters.
            if (ReferenceEquals(candidate, recipient) ||
                string.Equals(candidate.Username, recipient.Username, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Filter blocked senders at dispatch time.
            if (recipient.IsBlocked(candidate.Username))
            {
                continue;
            }

            var score = Score(candidate.Info, recipient.Info);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static int Score(PersonInfo candidate, PersonInfo recipient)
    {
        var score = 0;

        if (string.Equals(candidate.City, recipient.City, StringComparison.OrdinalIgnoreCase))
        {
            score += SameCityBonus;
        }

        if (Math.Abs(candidate.Age - recipient.Age) <= SimilarAgeThreshold)
        {
            score += SimilarAgeBonus;
        }

        score += CryptoRandom.GetInt32(RandomFactorMinInclusive, RandomFactorMaxExclusive);
        return score;
    }

    private static LoveLetter BuildLetter(PersonInfo sender)
    {
        var messageIndex = CryptoRandom.GetInt32(0, Messages.Length);
        var hidePhone = messageIndex == NotInterestedMessageIndex;

        return new LoveLetter
        {
            SenderUsername = sender.Username,
            SenderCity = sender.City,
            SenderAge = sender.Age,
            SenderPhone = hidePhone ? null : sender.Phone,
            Message = Messages[messageIndex],
        };
    }
}
