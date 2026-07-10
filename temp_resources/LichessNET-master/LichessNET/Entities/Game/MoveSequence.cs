namespace LichessNET.Entities.Game;

/// <summary>
/// Basically the notation part of a PGN game
/// </summary>
public class MoveSequence
{
    public List<Move> Moves { get; set; } = new List<Move>();
    public string OriginalPgn { get; set; }
}