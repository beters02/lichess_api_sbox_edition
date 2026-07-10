namespace LichessNET.Entities.Puzzle.Dashboard;

public class PuzzleDashboard
{
    public int Days { get; set; }
    public GlobalPuzzlePerformance Global { get; set; }
    public Dictionary<string, PuzzleTheme> Themes { get; set; }
}