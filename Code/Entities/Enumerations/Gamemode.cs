using System.Runtime.Serialization;

namespace LichessNET.Entities.Enumerations;

/// <summary>
/// Represents the various game modes available in Lichess.
/// This also includes all time controls for standard chess, which makes this
/// different than the <see cref="ChessVariant"/> enumeration.
/// </summary>
public enum Gamemode
{
    [EnumMember(Value = "bullet")] Bullet,
    [EnumMember(Value = "blitz")] Blitz,
    [EnumMember(Value = "rapid")] Rapid,
    [EnumMember(Value = "classical")] Classical,
    [EnumMember(Value = "chess960")] Chess960,
    [EnumMember(Value = "kingOfTheHill")] KingOfTheHill,
    [EnumMember(Value = "threeCheck")] ThreeCheck,
    [EnumMember(Value = "antichess")] Antichess,
    [EnumMember(Value = "atomic")] Atomic,
    [EnumMember(Value = "horde")] Horde,
    [EnumMember(Value = "racingKings")] RacingKings,
    [EnumMember(Value = "crazyhouse")] Crazyhouse,
    Storm, 
    Racer,
    Streak,
    UltraBullet,
    Correspondence,
    Puzzle
}