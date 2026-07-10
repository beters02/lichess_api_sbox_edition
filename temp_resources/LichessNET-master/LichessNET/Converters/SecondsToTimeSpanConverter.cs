using System.Text.Json;
using System.Text.Json.Serialization;
using LichessNET.Entities.Stats;

namespace LichessNET.Converters;

public class SecondsToTimeSpanConverter : JsonConverter<Playtime>
{
    public override Playtime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Playtime playtime = new Playtime();
        playtime.TotalSpan = TimeSpan.Zero;
        playtime.TvSpan = TimeSpan.Zero;
        
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var stats = doc.RootElement.EnumerateObject();
            foreach (JsonProperty property in stats)
            {
                if (property.Name == "total")
                {
                    playtime.TotalSpan = TimeSpan.FromSeconds(property.Value.GetInt64());
                }
                else if (property.Name == "tv")
                {
                    playtime.TvSpan = TimeSpan.FromSeconds(property.Value.GetInt64());
                }
            }
        }
        
        return playtime;
    }

    public override void Write(Utf8JsonWriter writer, Playtime value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}