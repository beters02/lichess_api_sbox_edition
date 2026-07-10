using LichessNET.Entities.Enumerations;
using LichessNET.Internal;

namespace LichessNET.Extensions;

internal static class EnumExtensions
{
    public static string ToApiName<T>(this T value) where T : Enum
    {
        return GetApiName(value);
    }

    public static string GetApiName<T>(this T enumValue) where T : Enum
    {
        return enumValue switch
        {
            Gamemode value => LichessEnumNames.ToApiName(value),
            ChessVariant value => LichessEnumNames.ToApiName(value),
            ChallengeDeniedReason value => LichessEnumNames.ToApiName(value),
            _ => enumValue.ToString().ToLowerInvariant()
        };
    }
}


