namespace LichessNET.Entities.Enumerations;

public enum GameStatus
{
    Created,
    Started,
    Aborted,
    Mate,
    Resign,
    Stalemate,
    Timeout,
    Draw,
    OutOfTime,
    Cheat,
    NoStart,
    UnknownFinish,
    VariantEnd,
}