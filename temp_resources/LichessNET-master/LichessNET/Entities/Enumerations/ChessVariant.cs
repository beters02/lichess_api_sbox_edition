using System.Runtime.Serialization;

namespace LichessNET.Entities.Enumerations;

public enum ChessVariant
{
    [EnumMember(Value = "standard")] Standard,
    [EnumMember(Value = "chess960")] Chess960,
    [EnumMember(Value = "kingOfTheHill")] KingOfTheHill,
    [EnumMember(Value = "threeCheck")] ThreeCheck,
    [EnumMember(Value = "antichess")] Antichess,
    [EnumMember(Value = "atomic")] Atomic,
    [EnumMember(Value = "horde")] Horde,
    [EnumMember(Value = "racingKings")] RacingKings,
    [EnumMember(Value = "crazyhouse")] Crazyhouse,
    [EnumMember(Value = "puzzle")] Storm,
    [EnumMember(Value = "puzzle")] Racer,
    [EnumMember(Value = "puzzle")] Streak
}