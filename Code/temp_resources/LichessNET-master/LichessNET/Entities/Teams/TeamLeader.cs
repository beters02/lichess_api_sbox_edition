namespace LichessNET.Entities.Social;

/// <summary>
/// Represents a leader of a team on Lichess, providing properties for flair, identification, name, and patron status.
/// </summary>
public class TeamLeader
{
    public string Flair { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Patron { get; set; }
}