namespace ChaoticCupid.Server.Contracts;

/// <summary>
/// Cupid-facing contract. Owns all matchmaking and dispatch logic; this is kept
/// completely separate from the person-facing surface.
/// </summary>
public interface ICupidService
{
    /// <summary>
    /// Runs one matchmaking round: scores candidates for every eligible
    /// recipient and dispatches at most one letter per recipient.
    /// </summary>
    Task DispatchLetters(CancellationToken cancellationToken);
}
