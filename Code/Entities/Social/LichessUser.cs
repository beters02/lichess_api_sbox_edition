using System.Text.Json.Serialization;
using LichessNET.Converters;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Interfaces;
using LichessNET.Entities.Stats;
using Newtonsoft.Json.Linq;

namespace LichessNET.Entities.Social;

/// <summary>
///     The complete information of a lichess user.
///     This class also contains information on which the client doesn't have access to.
///     In this case, the client will return null on such attributes.
/// </summary>
public class LichessUser
{
    /// <summary>
    ///     User ID, often it is the Username written in lowercase
    /// </summary>
    public string Id { get; set; } = String.Empty;

    /// <summary>
    ///     The username of the user
    /// </summary>
    public string Username { get; set; } = "Anonymous";

    /// <summary>
    ///     If the data is fetched in the request, the ratings will be set here.
    ///     The dictionary only contains those gamemodes as key, which were fetched.
    /// </summary>
    [JsonPropertyName("perfs")]
    [JsonConverter(typeof(GameStatsConverter))]
    public Dictionary<Gamemode, IGameStats>? Ratings { get; set; }

    /// <summary>
    ///     Current flair of the user
    /// </summary>
    public string? Flair { get; set; }
    
    [JsonConverter(typeof(MillisecondUnixConverter))]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    ///     Will be set to true if the user profile is disabled
    /// </summary>
    public bool? Disabled { get; set; }

    /// <summary>
    ///     Will be set to true if the account is flagged for TOS violations
    /// </summary>
    public bool TosViolation { get; set; }

    /// <summary>
    ///     The LichessProfile of the user
    /// </summary>
    public LichessProfile? Profile { get; set; }

    [JsonConverter(typeof(MillisecondUnixConverter))]
    public DateTime? SeenAt { get; set; }

    /// <summary>
    ///     If set to true, this user is an active patron of lichess
    /// </summary>
    public bool Patron { get; set; }

    /// <summary>
    ///     Set to true if the user is a verfied user
    /// </summary>
    public bool? Verified { get; set; }

    /// <summary>
    ///     The total playtime
    /// </summary>
    [JsonConverter(typeof(SecondsToTimeSpanConverter))]
    public Playtime PlayTime { get; set; }

    /// <summary>
    ///     Title of this user as string
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Title? Title { get; set; }

    /// <summary>
    ///     The game count stats
    /// </summary>
    public GameCounts? Count { get; set; }

    /// <summary>
    ///     Returns if the user is streaming
    /// </summary>
    public bool? Streaming { get; set; }


    public string? Url { get; set; }
    public string? Playing { get; set; }
    /// <summary>
    ///     Set to true if the user allows being followed
    /// </summary>
    public bool? Followable { get; set; }

    //TODO Add streamer property
    
    /// <summary>
    ///     Set to true if the user is following
    /// </summary>
    public bool? Following { get; set; }

    /// <summary>
    ///     If the user blocks the request
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    ///     Set to true if the user follows the user represented by the LichessClient
    /// </summary>
    public bool? FollowsYou { get; set; }

    public static Dictionary<Gamemode, GamemodeStats> DeserializeRatings(JToken json)
    {
        var dictionary = new Dictionary<Gamemode, GamemodeStats>();

        foreach (var perf in json.ToObject<Dictionary<string, GamemodeStats>>())
        {
            switch (perf.Key.ToLower())
            {
                case "bullet": dictionary.Add(Gamemode.Bullet, perf.Value); break;
                case "blitz": dictionary.Add(Gamemode.Blitz, perf.Value); break;
                case "rapid": dictionary.Add(Gamemode.Rapid, perf.Value); break;
                case "classical": dictionary.Add(Gamemode.Classical, perf.Value); break;
                case "atomic": dictionary.Add(Gamemode.Atomic, perf.Value); break;
                case "antichess": dictionary.Add(Gamemode.Antichess, perf.Value); break;
                case "chess960": dictionary.Add(Gamemode.Chess960, perf.Value); break;
                case "kingofthehill": dictionary.Add(Gamemode.KingOfTheHill, perf.Value); break;
                case "threecheck": dictionary.Add(Gamemode.ThreeCheck, perf.Value); break;
                case "horde": dictionary.Add(Gamemode.Horde, perf.Value); break;
                case "racingkings": dictionary.Add(Gamemode.RacingKings, perf.Value); break;
                case "crazyhouse": dictionary.Add(Gamemode.Crazyhouse, perf.Value); break;
                case "storm": dictionary.Add(Gamemode.Storm, perf.Value); break;
                case "racer": dictionary.Add(Gamemode.Racer, perf.Value); break;
                case "streak": dictionary.Add(Gamemode.Streak, perf.Value); break;
            }
        }

        return dictionary;
    }
}