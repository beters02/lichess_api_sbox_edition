#nullable enable annotations

namespace LichessNET.Entities.Puzzle;

/// <summary>
/// Represents a chess puzzle obtained from Lichess, including its metadata and solution.
/// </summary>
public class Puzzle
{
    /// <summary>
    /// Puzzle ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The game in which the puzzle was extracted from
    /// </summary>
    public PuzzleGame Game { get; set; }

    /// <summary>
    /// The ply in which the puzzle starts
    /// </summary>
    public int InitialPly { get; set; }

    /// <summary>
    /// The puzzle position after the opponent's setup move.
    /// </summary>
    public string Fen { get; set; }

    /// <summary>
    /// The opponent move that produced <see cref="Fen"/>.
    /// </summary>
    public string LastMove { get; set; }

    /// <summary>
    /// How often that puzzle was already played
    /// </summary>
    public int Plays { get; set; }

    /// <summary>
    /// The Rating of the puzzle
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// The moves of the puzzle
    /// </summary>
    public List<string> Solution { get; set; } = new List<string>();

    /// <summary>
    /// The topics that the puzzle covers
    /// </summary>
    public List<string> Themes { get; set; } = new List<string>();
}

