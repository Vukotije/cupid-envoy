namespace ChaoticCupid.Shared;

/// <summary>
/// Strongly-typed callback contract the server uses to push letters to a
/// subscribed person (the PubSub "subscriber" side).
/// </summary>
public interface ILetterClient
{
    Task ReceiveLetter(LoveLetter letter);
}
