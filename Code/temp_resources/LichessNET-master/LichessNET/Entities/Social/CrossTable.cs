namespace LichessNET.Entities.Social;

/// <summary>
/// Represents a cross table that summarizes the results of games between two players.
/// </summary>
public class CrossTable
{
    public int TotalGames { get; set; }
    public Dictionary<string, double> Scores { get; set; }
    public Matchup? CurrentMatchup { get; set; }
}