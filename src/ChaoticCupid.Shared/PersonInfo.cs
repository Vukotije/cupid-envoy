namespace ChaoticCupid.Shared;

/// <summary>
/// Plain data object describing a registered person. Carries no business logic.
/// </summary>
public sealed class PersonInfo
{
    public string Username { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Phone { get; set; } = string.Empty;
}
