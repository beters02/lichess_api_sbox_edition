using System.Reflection;
using System.Runtime.Serialization;

namespace LichessNET.Extensions;

internal static class EnumExtensions
{
    public static string? ToEnumMember<T>(this T value) where T : Enum
    {
        return typeof(T)
            .GetTypeInfo()
            .DeclaredMembers
            .SingleOrDefault(x => x.Name == value.ToString())?
            .GetCustomAttribute<EnumMemberAttribute>(false)?
            .Value;
    }

    public static string GetEnumMemberValue<T>(this T enumValue) where T : Enum
    {
        var type = typeof(T);
        var memberInfo = type.GetMember(enumValue.ToString());
        var attribute =
            memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault() as
                EnumMemberAttribute;
        return attribute?.Value;
    }
}