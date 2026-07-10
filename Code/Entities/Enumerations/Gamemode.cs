#nullable enable annotations

namespace LichessNET.Entities.Enumerations;

/// <summary>
/// Represents the various game modes available in Lichess.
/// This also includes all time controls for standard chess, which makes this
/// different than the <see cref="ChessVariant"/> enumeration.
/// </summary>
public enum Gamemode
{ Bullet, Blitz, Rapid, Classical, Chess960, KingOfTheHill, ThreeCheck, Antichess, Atomic, Horde, RacingKings, Crazyhouse,
    Storm, 
    Racer,
    Streak,
    UltraBullet,
    Correspondence,
    Puzzle
}


