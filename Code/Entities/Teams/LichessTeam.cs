using LichessNET.Entities.Social;

namespace LichessNET.Entities.Teams;

/// <summary>
/// Represents a team on Lichess, containing information such as its ID, name, description, and members.
/// </summary>
public class LichessTeam
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Flair { get; set; }
    public TeamLeader Leader { get; set; }
    public List<TeamLeader> Leaders { get; set; }
    public int NbMembers { get; set; }
    public bool Open { get; set; }
    public bool Joined { get; set; }
    public bool Requested { get; set; }
}