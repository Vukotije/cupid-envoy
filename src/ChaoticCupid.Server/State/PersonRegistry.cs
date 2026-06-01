using System.Collections.Concurrent;
using ChaoticCupid.Shared;

namespace ChaoticCupid.Server.State;

/// <summary>
/// Thread-safe store of all registered persons. Shared between the hub (writers)
/// and the Cupid background service (reader). Registered as a singleton.
/// </summary>
public sealed class PersonRegistry
{
    private readonly ConcurrentDictionary<string, RegisteredPerson> _peopleByUsername =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _usernameByConnection =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Registers (or re-registers) a person for the given connection. Returns
    /// false if the username is already taken by a different live connection.
    /// </summary>
    public bool TryRegister(string connectionId, PersonInfo info, out RegisteredPerson? person)
    {
        person = null;

        var entry = new RegisteredPerson(info, connectionId);
        if (!_peopleByUsername.TryAdd(info.Username, entry))
        {
            return false;
        }

        _usernameByConnection[connectionId] = info.Username;
        person = entry;
        return true;
    }

    public bool TryGetByConnection(string connectionId, out RegisteredPerson? person)
    {
        person = null;
        return _usernameByConnection.TryGetValue(connectionId, out var username)
               && _peopleByUsername.TryGetValue(username, out person);
    }

    /// <summary>Removes the person associated with a dropped connection, if any.</summary>
    public void RemoveByConnection(string connectionId)
    {
        if (_usernameByConnection.TryRemove(connectionId, out var username))
        {
            _peopleByUsername.TryRemove(username, out _);
        }
    }

    /// <summary>Point-in-time list of registered persons for a matchmaking round.</summary>
    public IReadOnlyList<RegisteredPerson> Snapshot() => _peopleByUsername.Values.ToArray();
}
