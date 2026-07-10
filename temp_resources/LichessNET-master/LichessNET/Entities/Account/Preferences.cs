namespace LichessNET.Entities.Account;

/// <summary>
///     Represents the visual and gameplay preferences settings for a user on the Lichess platform.
/// </summary>
public class Preferences
{
    public bool Dark { get; set; }
    public bool Transp { get; set; }
    public string BgImg { get; set; }
    public bool Is3d { get; set; }
    public string Theme { get; set; }
    public string PieceSet { get; set; }
    public string Theme3d { get; set; }
    public string PieceSet3d { get; set; }
    public string SoundSet { get; set; }
    public int AutoQueen { get; set; }
    public int AutoThreefold { get; set; }
    public int Takeback { get; set; }
    public int Moretime { get; set; }
    public int ClockTenths { get; set; }
    public bool ClockBar { get; set; }
    public bool ClockSound { get; set; }
    public bool Premove { get; set; }
    public int Animation { get; set; }
    public bool Captured { get; set; }
    public bool Follow { get; set; }
    public bool Highlight { get; set; }
    public bool Destination { get; set; }
    public int Coords { get; set; }
    public int Replay { get; set; }
    public int Challenge { get; set; }
    public int Message { get; set; }
    public int SubmitMove { get; set; }
    public int ConfirmResign { get; set; }
    public int InsightShare { get; set; }
    public int KeyboardMove { get; set; }
    public int Zen { get; set; }
    public int Ratings { get; set; }
    public int MoveEvent { get; set; }
    public int RookCastle { get; set; }
}