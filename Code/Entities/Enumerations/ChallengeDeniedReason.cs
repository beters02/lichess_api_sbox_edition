using System.Runtime.Serialization;

namespace LichessNET.Entities.Enumerations;

public enum ChallengeDeniedReason
{
    [EnumMember(Value = "generic")] Generic,
    [EnumMember(Value = "later")] Later,
    [EnumMember(Value = "tooFast")] TooFast,
    [EnumMember(Value = "tooSlow")] TooSlow,
    [EnumMember(Value = "timeControl")] TimeControl,
    [EnumMember(Value = "rated")] Rated,
    [EnumMember(Value = "casual")] Casual,
    [EnumMember(Value = "standard")] Standard,
    [EnumMember(Value = "variant")] Variant,
    [EnumMember(Value = "noBot")] NoBot,
    [EnumMember(Value = "onlyBot")] OnlyBot
}