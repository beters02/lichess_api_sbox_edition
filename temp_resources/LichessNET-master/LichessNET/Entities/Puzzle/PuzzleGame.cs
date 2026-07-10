using LichessNET.Entities.Game;

namespace LichessNET.Entities.Puzzle;

/// <summary>
/// This class represents a game used to extract a puzzle from.
/// </summary>
public class PuzzleGame
{
    public string Clock { get; set; }
    public string Id { get; set; }
    public Variant Perf { get; set; }
    public string PGN { get; set; }
    public bool Rated { get; set; }
}