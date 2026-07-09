using System.Text.Json.Serialization;
using LichessNET.Entities.Interfaces;

namespace LichessNET.Entities.Stats;

/// <summary>
///     This class contains all stats of a user in a specific gamemode
/// </summary>
public class GamemodeStats : IGameStats
{
    /// <summary>
    ///     Amount of games played in this gamemode
    /// </summary>
    public int Games { get; set; }

    /// <summary>
    ///     The current rating of the user in this gamemode
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    ///     The current rating deviation of the user in this gamemode
    /// </summary>
    public int Rd { get; set; }

    /// <summary>
    ///     The current progress of the user in this gamemode over the last 12 games
    /// </summary>
    [JsonPropertyName("prog")]
    public int Progress { get; set; }

    /// <summary>
    ///     Is the rating considered provisional?
    /// </summary>
    public bool Prov { get; set; }
}