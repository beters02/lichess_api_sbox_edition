namespace LichessNET.Entities.Stats;

/// <summary>
///     This class contains the game counts of a user in the structure fetched from the lichess API
/// </summary>
public class GameCounts
{
    public int All { get; set; }
    public int Rated { get; set; }
    public int Ai { get; set; }
    public int Draw { get; set; }
    public int DrawH { get; set; }
    public int Loss { get; set; }
    public int LossH { get; set; }
    public int Win { get; set; }
    public int WinH { get; set; }
    public int Bookmark { get; set; }
    public int Playing { get; set; }
    public int Import { get; set; }
    public int Me { get; set; }
}