#nullable enable annotations

using System.Text.Json;
using LichessNET.Converters;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Interfaces;
using LichessNET.Entities.Stats;

namespace LichessNET.Entities.Social;

/// <summary>
///     The complete information of a lichess user.
/// </summary>
public class LichessUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = "Anonymous";

    [JsonPropertyName("perfs")]
    [JsonConverter(typeof(GameStatsConverter))]
    public Dictionary<Gamemode, IGameStats>? Ratings { get; set; }

    public string? Flair { get; set; }

    [JsonConverter(typeof(MillisecondUnixConverter))]
    public DateTime? CreatedAt { get; set; }

    public bool? Disabled { get; set; }
    public bool TosViolation { get; set; }
    public LichessProfile? Profile { get; set; }

    [JsonConverter(typeof(MillisecondUnixConverter))]
    public DateTime? SeenAt { get; set; }

    public bool Patron { get; set; }
    public bool? Verified { get; set; }

    [JsonConverter(typeof(SecondsToTimeSpanConverter))]
    public Playtime PlayTime { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Title? Title { get; set; }

    public GameCounts? Count { get; set; }
    public bool? Streaming { get; set; }
    public string? Url { get; set; }
    public string? Playing { get; set; }
    public bool? Followable { get; set; }
    public bool? Following { get; set; }
    public bool? Blocking { get; set; }
    public bool? FollowsYou { get; set; }

    public static Dictionary<Gamemode, GamemodeStats> DeserializeRatings(JsonElement json)
    {
        var dictionary = new Dictionary<Gamemode, GamemodeStats>();
        foreach (var perf in json.EnumerateObject())
        {
            var stats = perf.Value.Deserialize<GamemodeStats>(LichessJson.Options);
            if (stats == null)
                continue;

            switch (perf.Name.ToLowerInvariant())
            {
                case "bullet": dictionary.Add(Gamemode.Bullet, stats); break;
                case "blitz": dictionary.Add(Gamemode.Blitz, stats); break;
                case "rapid": dictionary.Add(Gamemode.Rapid, stats); break;
                case "classical": dictionary.Add(Gamemode.Classical, stats); break;
                case "atomic": dictionary.Add(Gamemode.Atomic, stats); break;
                case "antichess": dictionary.Add(Gamemode.Antichess, stats); break;
                case "chess960": dictionary.Add(Gamemode.Chess960, stats); break;
                case "kingofthehill": dictionary.Add(Gamemode.KingOfTheHill, stats); break;
                case "threecheck": dictionary.Add(Gamemode.ThreeCheck, stats); break;
                case "horde": dictionary.Add(Gamemode.Horde, stats); break;
                case "racingkings": dictionary.Add(Gamemode.RacingKings, stats); break;
                case "crazyhouse": dictionary.Add(Gamemode.Crazyhouse, stats); break;
                case "storm": dictionary.Add(Gamemode.Storm, stats); break;
                case "racer": dictionary.Add(Gamemode.Racer, stats); break;
                case "streak": dictionary.Add(Gamemode.Streak, stats); break;
            }
        }

        return dictionary;
    }
}

