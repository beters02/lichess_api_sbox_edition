namespace LichessNET.Entities.Social;

/// <summary>
/// Represents streaming information related to a user, containing details of Twitch and YouTube channels.
/// </summary>
public class StreamingInfo
{
    /// <summary>
    ///     The link to the Twitch Account of the user
    /// </summary>
    public string? Twitch { get; set; }

    /// <summary>
    ///     The link to the YouTube Account of the user
    /// </summary>
    public string? YouTube { get; set; }
}