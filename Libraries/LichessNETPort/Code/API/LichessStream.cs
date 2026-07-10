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

    public async Task StreamGameAsync()
    {
        LichessStreamCounter++;
        if (LichessStreamCounter > 5)
        {
            _logger.Warning("There are already " + LichessStreamCounter +
                            " active streams. The maximum number of streams per IP on Lichess is 8.");
        }

        while (LichessStreamCounter >= 8)
        {
            _logger.Error("The maximum of streams for lichess is reached. This stream waits until another stream is closed.");
            await Task.Delay(1000);
        }

        var content = await Sandbox.Http.RequestStringAsync(_requestUri, _method, null, null, CancellationToken.None);
        foreach (var line in content.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var json = LichessJson.Parse(line.Trim());
            GameUpdateReceived?.Invoke(this, json);
        }

        LichessStreamCounter--;
    }

    private static string CreateStreamId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

