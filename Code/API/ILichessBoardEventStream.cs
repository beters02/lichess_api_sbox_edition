#nullable enable annotations

namespace LichessNET.API;

/// <summary>
/// An unstarted Lichess board event stream.
/// </summary>
public interface ILichessBoardEventStream : IAsyncDisposable
{
    event Action<ILichessBoardEventStream, JsonElement>? LineReceived;
    event Action<ILichessBoardEventStream, Exception>? ErrorReceived;
    event Action<ILichessBoardEventStream>? Completed;

    Task Completion { get; }
    Task Ready { get; }

    void Start();
}
