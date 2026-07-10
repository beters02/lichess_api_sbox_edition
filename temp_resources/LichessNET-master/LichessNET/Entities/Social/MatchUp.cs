namespace LichessNET.Entities.Social;

/// <summary>
/// Represents the matchup between two players, including the total number of games played and their respective scores.
/// </summary>
public class Matchup
{
    /// <summary>
    /// Gets or sets the total number of games played in the matchup between two players.
    /// </summary>
    public int TotalGames { get; set; }

    public Dictionary<string, int> Scores { get; set; }
}