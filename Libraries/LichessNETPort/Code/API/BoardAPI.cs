using LichessNET.Entities.Board;
using LichessNET.Gameplay;

namespace LichessNET.API;

public partial class LichessApiClient
{
    public async Task CreateBoardSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var request = GetRequestScaffold("api/board/seek");
        await SendRequest(request, "POST", formData: options.ToFormData(), formDataAsContent: true,
            cancellationToken: cancellationToken);
    }

    public async Task<LichessNdjsonStream> StreamBoardAccountEventsAsync(CancellationToken cancellationToken = default)
    {
        var request = GetRequestScaffold("api/stream/event");
        await _ratelimitController.Consume("api/stream/event", true);

        var stream = new LichessNdjsonStream(request.BuildUri(), "GET", GetRequestHeaders(request), cancellationToken);
        stream.Start();
        return stream;
    }

    public async Task<LichessNdjsonStream> StreamBoardGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game id is required.", nameof(gameId));

        var request = GetRequestScaffold("api/board/game/stream/" + Uri.EscapeDataString(gameId));
        await _ratelimitController.Consume("api/board/game/stream", true);

        var stream = new LichessNdjsonStream(request.BuildUri(), "GET", GetRequestHeaders(request), cancellationToken);
        stream.Start();
        return stream;
    }

    public async Task<bool> MakeBoardMoveAsync(string gameId, string uci, bool offerDraw = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game id is required.", nameof(gameId));

        if (!UciMove.TryParse(uci, out var move))
            throw new ArgumentException("Move must be valid UCI notation, for example e2e4 or e7e8q.", nameof(uci));

        var endpoint = "api/board/game/" + Uri.EscapeDataString(gameId) + "/move/" + Uri.EscapeDataString(move.ToString());
        var request = offerDraw
            ? GetRequestScaffold(endpoint, Tuple.Create("offeringDraw", "true"))
            : GetRequestScaffold(endpoint);

        return await ReadBoardOkAsync(await SendRequest(request, "POST", cancellationToken: cancellationToken));
    }

    public async Task<bool> AbortBoardGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        return await PostBoardGameActionAsync(gameId, "abort", cancellationToken);
    }

    public async Task<bool> ResignBoardGameAsync(string gameId, CancellationToken cancellationToken = default)
    {
        return await PostBoardGameActionAsync(gameId, "resign", cancellationToken);
    }

    public async Task<bool> HandleDrawOfferAsync(string gameId, bool accept, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game id is required.", nameof(gameId));

        var endpoint = "api/board/game/" + Uri.EscapeDataString(gameId) + "/draw/" + (accept ? "yes" : "no");
        var request = GetRequestScaffold(endpoint);
        return await ReadBoardOkAsync(await SendRequest(request, "POST", cancellationToken: cancellationToken));
    }

    public async Task<bool> SendBoardChatAsync(string gameId, string text, BoardChatRoom room = BoardChatRoom.Player,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game id is required.", nameof(gameId));

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Chat text is required.", nameof(text));

        var request = GetRequestScaffold("api/board/game/" + Uri.EscapeDataString(gameId) + "/chat");
        var form = new Dictionary<string, string>
        {
            ["room"] = room == BoardChatRoom.Spectator ? "spectator" : "player",
            ["text"] = text
        };

        return await ReadBoardOkAsync(await SendRequest(request, "POST", formData: form, formDataAsContent: true,
            cancellationToken: cancellationToken));
    }

    private async Task<bool> PostBoardGameActionAsync(string gameId, string action, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game id is required.", nameof(gameId));

        var request = GetRequestScaffold("api/board/game/" + Uri.EscapeDataString(gameId) + "/" + action);
        return await ReadBoardOkAsync(await SendRequest(request, "POST", cancellationToken: cancellationToken));
    }

    private static async Task<bool> ReadBoardOkAsync(LichessResponse response)
    {
        if (!response.IsSuccessStatusCode)
            return false;

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
            return true;

        try
        {
            var json = LichessJson.Parse(content);
            return json.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True;
        }
        catch
        {
            return content.Contains("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
