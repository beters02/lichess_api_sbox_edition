#nullable enable annotations

namespace LichessNET.API;

/// <summary>
/// Represents a stream sent by Lichess.
/// </summary>
public class LichessStream
{
    public delegate void GameUpdateEventHandler(object sender, JsonElement gameUpdate);

    private static int LichessStreamCounter;
    private readonly LichessLog _logger;
    private readonly string _method;
    private readonly string _requestUri;
    private readonly string _streamId;

    public LichessStream(string requestUrl, string method = "GET")
    {
        _requestUri = requestUrl;
        _method = method;
        _streamId = CreateStreamId();
        _logger = new LichessLog("LichessStream_" + _streamId);
    }

    public event GameUpdateEventHandler GameUpdateReceived;

    public Task StreamGameAsync()
    {
        return StreamGameAsync(CancellationToken.None);
    }

    public async Task StreamGameAsync(CancellationToken cancellationToken)
    {
        var streamCountIncremented = false;
        try
        {
            while (LichessStreamCounter >= 8)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.Error("The maximum of streams for lichess is reached. This stream waits until another stream is closed.");
                await Task.Delay(1000, cancellationToken);
            }

            LichessStreamCounter++;
            streamCountIncremented = true;
            if (LichessStreamCounter > 5)
            {
                _logger.Warning("There are already " + LichessStreamCounter +
                                " active streams. The maximum number of streams per IP on Lichess is 8.");
            }

            var stream = await Sandbox.Http.RequestStreamAsync(_requestUri, _method, null, null,
                cancellationToken);
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var json = LichessJson.Parse(line.Trim());
                    GameUpdateReceived?.Invoke(this, json);
                }
            }
        }
        finally
        {
            if (streamCountIncremented)
                LichessStreamCounter--;
        }
    }

    private static string CreateStreamId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

