#nullable enable annotations

namespace LichessNET.API;

/// <summary>
/// Reads Lichess NDJSON streams line-by-line and raises parsed JSON events.
/// </summary>
public sealed class LichessNdjsonStream : IAsyncDisposable
{
    private const int WarningStreamCount = 5;
    private const int MaxStreamCount = 8;
    private static readonly object StreamCountLock = new();
    private static int ActiveStreamCount;

    private readonly CancellationTokenSource _cancellation;
    private readonly Dictionary<string, string> _headers;
    private readonly LichessLog _logger;
    private readonly string _method;
    private readonly string _requestUri;
    private readonly string _streamId;
    private Task _completion = Task.CompletedTask;
    private bool _started;

    internal LichessNdjsonStream(string requestUri, string method, Dictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        _requestUri = requestUri;
        _method = method ?? "GET";
        _headers = headers ?? new Dictionary<string, string>();
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _streamId = CreateStreamId();
        _logger = new LichessLog("LichessNdjsonStream_" + _streamId);
    }

    public event Action<LichessNdjsonStream, JsonElement> LineReceived;
    public event Action<LichessNdjsonStream, Exception> ErrorReceived;
    public event Action<LichessNdjsonStream> Completed;

    public Task Completion => _completion;

    public void Start()
    {
        if (_started)
            return;

        _started = true;
        _completion = RunAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_cancellation.IsCancellationRequested)
            _cancellation.Cancel();

        try
        {
            await _completion;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cancellation.Dispose();
        }
    }

    private async Task RunAsync()
    {
        await WaitForSlotAsync();
        IncrementStreamCount();

        try
        {
            var stream = await Sandbox.Http.RequestStreamAsync(_requestUri, _method, null, _headers, _cancellation.Token);
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var json = LichessJson.Parse(line.Trim());
                        LineReceived?.Invoke(this, json);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning("Failed to parse stream line: " + ex.Message);
                        ErrorReceived?.Invoke(this, ex);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.Error("Stream failed: " + ex.Message);
            ErrorReceived?.Invoke(this, ex);
        }
        finally
        {
            DecrementStreamCount();
            Completed?.Invoke(this);
        }
    }

    private async Task WaitForSlotAsync()
    {
        while (GetActiveStreamCount() >= MaxStreamCount && !_cancellation.IsCancellationRequested)
        {
            _logger.Error("The maximum number of Lichess streams is reached. Waiting for another stream to close.");
            await Task.Delay(1000, _cancellation.Token);
        }
    }

    private void IncrementStreamCount()
    {
        lock (StreamCountLock)
        {
            ActiveStreamCount++;
            if (ActiveStreamCount > WarningStreamCount)
            {
                _logger.Warning("There are already " + ActiveStreamCount +
                                " active streams. The maximum number of streams per IP on Lichess is 8.");
            }
        }
    }

    private static void DecrementStreamCount()
    {
        lock (StreamCountLock)
        {
            ActiveStreamCount = Math.Max(0, ActiveStreamCount - 1);
        }
    }

    private static int GetActiveStreamCount()
    {
        lock (StreamCountLock)
        {
            return ActiveStreamCount;
        }
    }

    private static string CreateStreamId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
