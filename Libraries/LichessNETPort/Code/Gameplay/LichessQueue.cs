using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Entities.Enumerations;

namespace LichessNET.Gameplay;

public sealed class LichessGameFoundEventArgs : EventArgs
{
    public LichessGameFoundEventArgs(string gameId, string color, BoardGameInfo game)
    {
        GameId = gameId;
        Color = color;
        Game = game;
    }

    public string GameId { get; }
    public string Color { get; }
    public BoardGameInfo Game { get; }
}

public sealed class LichessQueue : IAsyncDisposable
{
    private readonly LichessApiClient _api;
    private CancellationTokenSource _cancellation;
    private LichessNdjsonStream _accountStream;
    private Task _seekTask = Task.CompletedTask;

    public LichessQueue(LichessApiClient api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public LichessApiClient Api => _api;

    public event Action<LichessQueue, LichessGameFoundEventArgs> OnGameFound;
    public event Action<LichessQueue, Exception> OnError;

    public bool StopWhenGameFound { get; set; } = true;

    public async Task StartSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.ToFormData();
        await ValidatePlayTokenAsync();

        await StopAsync();
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _accountStream = await _api.StreamBoardAccountEventsAsync(_cancellation.Token);
        _accountStream.LineReceived += HandleAccountLine;
        _accountStream.ErrorReceived += HandleStreamError;

        _seekTask = RunSeekAsync(options, _cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (_cancellation != null && !_cancellation.IsCancellationRequested)
            _cancellation.Cancel();

        if (_accountStream != null)
            await _accountStream.DisposeAsync();

        try
        {
            await _seekTask;
        }
        catch (OperationCanceledException)
        {
        }

        _seekTask = Task.CompletedTask;
        _accountStream = null;
        _cancellation?.Dispose();
        _cancellation = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task RunSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken)
    {
        try
        {
            await _api.CreateBoardSeekAsync(options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private async Task ValidatePlayTokenAsync()
    {
        var token = _api.GetToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("A Lichess OAuth token with board:play is required.");

        var result = await _api.TestTokensAsync(new List<string> { token });
        if (!result.TryGetValue(token, out var info) || info == null || !info.IsAllowed(TokenPermission.PlayGames))
            throw new UnauthorizedAccessException("The Lichess OAuth token must include board:play.");
    }

    private void HandleAccountLine(LichessNdjsonStream stream, JsonElement data)
    {
        try
        {
            if (!BoardEventParser.GetEventType(data).Equals("gameStart", StringComparison.OrdinalIgnoreCase))
                return;

            var gameStart = BoardEventParser.ParseGameStart(data);
            var game = gameStart?.Game;
            var gameId = game?.BestGameId;
            if (string.IsNullOrWhiteSpace(gameId))
                return;

            OnGameFound?.Invoke(this, new LichessGameFoundEventArgs(gameId, game?.Color, game));

            if (StopWhenGameFound && _cancellation != null && !_cancellation.IsCancellationRequested)
                _cancellation.Cancel();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private void HandleStreamError(LichessNdjsonStream stream, Exception exception)
    {
        OnError?.Invoke(this, exception);
    }
}
