using System.Text.Json;
using System.Text.Json.Serialization;

namespace LichessNET.Converters;

public class MillisecondUnixConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var milliseconds = reader.GetInt64();
        DateTime unixEpoch = DateTime.UnixEpoch;
        return unixEpoch.Add(TimeSpan.FromMilliseconds(milliseconds));
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}