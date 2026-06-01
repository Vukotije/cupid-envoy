using ChaoticCupid.Shared;

namespace ChaoticCupid.Server.Contracts;

/// <summary>
/// Person-facing contract. These are the operations a connected person (the
/// console client) can invoke on the hub.
/// </summary>
public interface IPersonService
{
    /// <summary>Registers the calling connection as a single person looking for matches.</summary>
    Task InitSinglePerson(PersonInfo person);

    /// <summary>Confirms the current letter has been read, unblocking the next delivery.</summary>
    Task AcknowledgeLetter();

    /// <summary>Adds <paramref name="username"/> to the caller's personal block list.</summary>
    Task BlockUser(string username);
}
