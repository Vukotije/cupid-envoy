namespace ChaoticCupid.Shared;

/// <summary>
/// Payload delivered to a recipient. Phone is intentionally nullable: it is
/// omitted when the "not interested" message is selected.
/// </summary>
public sealed class LoveLetter
{
    public string SenderUsername { get; set; } = string.Empty;
    public string SenderCity { get; set; } = string.Empty;
    public int SenderAge { get; set; }

    /// <summary>Null when the sender's phone must not be revealed.</summary>
    public string? SenderPhone { get; set; }

    public string Message { get; set; } = string.Empty;
}
