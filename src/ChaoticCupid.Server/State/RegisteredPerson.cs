using ChaoticCupid.Shared;

namespace ChaoticCupid.Server.State;

/// <summary>
/// Server-side view of a registered person: their data plus the mutable,
/// thread-safe state Cupid needs (connection, pending acknowledgment, block list).
/// </summary>
public sealed class RegisteredPerson
{
    private readonly HashSet<string> _blockedUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _blockLock = new();

    // 0 = idle (may receive a letter), 1 = awaiting acknowledgment.
    private int _pendingAck;

    public RegisteredPerson(PersonInfo info, string connectionId)
    {
        Info = info;
        ConnectionId = connectionId;
    }

    public PersonInfo Info { get; }

    public string ConnectionId { get; set; }

    public string Username => Info.Username;

    public bool HasPendingLetter => Volatile.Read(ref _pendingAck) == 1;

    /// <summary>
    /// Atomically marks this person as awaiting acknowledgment. Returns false if
    /// they already have an unacknowledged letter (so no new one should be sent).
    /// </summary>
    public bool TryBeginDelivery() => Interlocked.CompareExchange(ref _pendingAck, 1, 0) == 0;

    /// <summary>Clears the pending flag so the next round can deliver again.</summary>
    public void AcknowledgeLetter() => Interlocked.Exchange(ref _pendingAck, 0);

    public void Block(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return;
        }

        lock (_blockLock)
        {
            _blockedUsers.Add(username.Trim());
        }
    }

    public bool IsBlocked(string username)
    {
        lock (_blockLock)
        {
            return _blockedUsers.Contains(username);
        }
    }
}
