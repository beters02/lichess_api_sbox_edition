using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Game;
using LichessNET.Entities.Interfaces;
using LichessNET.Entities.Stats;

namespace LichessNET.Converters;

public class GameStatsConverter : JsonConverter<Dictionary<Gamemode, IGameStats>>
{
    public override Dictionary<Gamemode, IGameStats>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var output = new Dictionary<Gamemode, IGameStats>();
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                Console.WriteLine(property.Name);
                if (Gamemode.TryParse(property.Name, true, out Gamemode gamemode))
                {
                    IGameStats? stats = null;
                    if (property.Value.TryGetProperty("games", out _))
                    {
                        stats = JsonSerializer.Deserialize<GamemodeStats>(property.Value, new JsonSerializerOptions(){PropertyNameCaseInsensitive = true});
                    }
                    else if (property.Value.TryGetProperty("runs", out _))
                    {
                        stats = JsonSerializer.Deserialize<RunStats>(property.Value, new JsonSerializerOptions(){PropertyNameCaseInsensitive = true});
                    }

                    if (stats != null)
                    {
                        output.Add(gamemode, stats);
                    }
                }
            }
            
        }

        return output;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<Gamemode, IGameStats> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}