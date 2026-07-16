#nullable enable annotations

using System.Text.Json;
using System.Text.Json.Serialization;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.OAuth;

namespace LichessNET.Converters;

public class PermissionJsonConverter : JsonConverter<List<TokenPermission>>
{
    public override List<TokenPermission>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new List<TokenPermission>();
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Lichess token scopes must be a string.");
        return TokenInfo.GetPermissions(reader.GetString() ?? "");
    }

    public override void Write(Utf8JsonWriter writer, List<TokenPermission> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

