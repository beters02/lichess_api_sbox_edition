#nullable enable annotations

namespace LichessNET.Gameplay;

public enum LichessGameConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Finished
}

public sealed class LichessGameSessionOptions
{
    private static readonly TimeSpan[] DefaultDelays =
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15)
    };

    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Delays used for consecutive automatic reconnect attempts. The final
    /// delay repeats for all later attempts.
    /// </summary>
    public IReadOnlyList<TimeSpan> ReconnectDelays { get; set; } = DefaultDelays.ToArray();

    public IReadOnlyList<TimeSpan> AutomaticReconnectDelays
    {
        get => ReconnectDelays;
        set => ReconnectDelays = value;
    }

    internal TimeSpan GetReconnectDelay(int attempt)
    {
        var delays = ReconnectDelays;
        if (delays == null || delays.Count == 0)
            throw new InvalidOperationException("At least one reconnect delay is required.");

        var index = Math.Min(Math.Max(0, attempt), delays.Count - 1);
        var delay = delays[index];
        if (delay < TimeSpan.Zero)
            throw new InvalidOperationException("Reconnect delays cannot be negative.");

        return delay;
    }
}
