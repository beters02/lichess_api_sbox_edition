namespace LichessNET.Entities.Game;

/// <summary>
/// Represents an ongoing game in Lichess.
/// </summary>
public class OngoingGame
{
    public string GameId { get; set; }
    public string FullId { get; set; }
    public string Color { get; set; }
    public string Fen { get; set; }
    public bool HasMoved { get; set; }
    public bool IsMyTurn { get; set; }
    public string LastMove { get; set; }
    public Opponent Opponent { get; set; }
    public string Perf { get; set; }
    public bool Rated { get; set; }
    public int SecondsLeft { get; set; }
    public string Source { get; set; }
    public string Speed { get; set; }
    public Variant Variant { get; set; }
    public int PlysAtInitFen { get; set; }
    public List<Move> Moves { get; set; }
}