#nullable enable annotations

namespace LichessNET.API;

/// <summary>
/// Reads Lichess NDJSON streams line-by-line and raises parsed JSON events.
/// </summary>
public sealed class LichessNdjsonStream : ILichessBoardEventStream
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

    private event Action<ILichessBoardEventStream, JsonElement>? InterfaceLineReceived;
    private event Action<ILichessBoardEventStream, Exception>? InterfaceErrorReceived;
    private event Action<ILichessBoardEventStream>? InterfaceCompleted;

    public event Action<LichessNdjsonStream, JsonElement> LineReceived;
    public event Action<LichessNdjsonStream, Exception> ErrorReceived;
    public event Action<LichessNdjsonStream> Completed;

    event Action<ILichessBoardEventStream, JsonElement>? ILichessBoardEventStream.LineReceived
    {
        add => InterfaceLineReceived += value;
        remove => InterfaceLineReceived -= value;
    }

    event Action<ILichessBoardEventStream, Exception>? ILichessBoardEventStream.ErrorReceived
    {
        add => InterfaceErrorReceived += value;
        remove => InterfaceErrorReceived -= value;
    }

    event Action<ILichessBoardEventStream>? ILichessBoardEventStream.Completed
    {
        add => InterfaceCompleted += value;
        remove => InterfaceCompleted -= value;
    }

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
        var streamCountIncremented = false;

        try
        {
            await WaitForSlotAsync();
            _cancellation.Token.ThrowIfCancellationRequested();

            IncrementStreamCount();
            streamCountIncremented = true;

            Stream stream;
            try
            {
                stream = await Sandbox.Http.RequestStreamAsync(_requestUri, _method, null, _headers,
                    _cancellation.Token);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new LichessApiException((exception as HttpRequestException)?.StatusCode);
            }

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(_cancellation.Token);
                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var json = LichessJson.Parse(line.Trim());
                        RaiseLineReceived(json);
                    }
                    catch (Exception exception)
                    {
                        _logger.Warning("Failed to parse stream line: " + exception.Message);
                        RaiseErrorReceived(exception);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (HttpRequestException exception)
        {
            var safeException = exception as LichessApiException
                                ?? new LichessApiException(exception.StatusCode);
            _logger.Error("Stream failed: " + safeException.Message);
            RaiseErrorReceived(safeException);
        }
        catch (Exception exception)
        {
            _logger.Error("Stream failed: " + exception.Message);
            RaiseErrorReceived(exception);
        }
        finally
        {
            if (streamCountIncremented)
                DecrementStreamCount();

            RaiseCompleted();
        }
    }

    private void RaiseLineReceived(JsonElement json)
    {
        LineReceived?.Invoke(this, json);
        InterfaceLineReceived?.Invoke(this, json);
    }

    private void RaiseErrorReceived(Exception exception)
    {
        ErrorReceived?.Invoke(this, exception);
        InterfaceErrorReceived?.Invoke(this, exception);
    }

    private void RaiseCompleted()
    {
        Completed?.Invoke(this);
        InterfaceCompleted?.Invoke(this);
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
