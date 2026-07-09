using LichessNET.Entities.Enumerations;

namespace LichessNET.Entities.Social;

/// <summary>
/// Represents the real-time status of a user on Lichess, including various attributes
/// such as online status, whether they are currently playing or streaming, and other details.
/// </summary>
public class UserRealTimeStatus
{
    public LichessUser User { get; set; } = new LichessUser();

    public string Name { get; set; } = "Anonymous";

    public string Id { get; set; } = "anonymous";

    public bool? Online { get; set; } = false;
    public bool? Playing { get; set; } = false;
    public bool? Streaming { get; set; } = false;
    public bool? Patron { get; set; } = false;
    internal int? Signal { get; set; } = 5;

    public SignalConnection SignalConnection
    {
        get
        {
            if (Signal == null) return SignalConnection.Unknown;
            return (SignalConnection)Signal;
        }
    }
}