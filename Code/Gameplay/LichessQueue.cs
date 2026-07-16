#nullable enable annotations

using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.OAuth;
using LichessNET.Internal;

namespace LichessNET.Gameplay;

public enum LichessQueueState
{
    Idle,
    ValidatingToken,
    Seeking,
    GameFound,
    Stopping,
    Faulted
}

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
    private readonly ILichessBoardClient _client;
    private readonly LichessLog _logger;
    private CancellationTokenSource? _cancellation;
    private ILichessBoardEventStream? _accountStream;
    private Task _seekTask = Task.CompletedTask;
    private bool _accountStreamErrorObserved;
    private LichessQueueState _state;

    public LichessQueue(LichessApiClient api)
        : this((ILichessBoardClient)api)
    {
    }

    public LichessQueue(ILichessBoardClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = new LichessLog("LichessQueue",
            client is LichessApiClient api && api.DebugEnabled);
    }

    /// <summary>
    /// The concrete client supplied to the legacy constructor.
    /// </summary>
    public LichessApiClient Api => _client as LichessApiClient
        ?? throw new InvalidOperationException("This queue uses a custom ILichessBoardClient.");

    public ILichessBoardClient BoardClient => _client;
    public ILichessBoardClient Client => _client;
    public LichessQueueState State => _state;
    public bool IsSeeking => _state == LichessQueueState.Seeking;

    public event Action<LichessQueue, LichessGameFoundEventArgs>? OnGameFound;
    public event Action<LichessQueue, Exception>? OnError;
    public event Action<LichessQueue, LichessQueueState>? OnStateChanged;

    public bool StopWhenGameFound { get; set; } = true;

    /// <summary>
    /// Validates the current in-memory token and returns its public metadata.
    /// </summary>
    public async Task<TokenInfo> ValidatePlayTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = _client.GetToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("A Lichess OAuth token with board:play is required.");

        var result = await _client.TestTokensAsync(new List<string> { token }, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (!result.TryGetValue(token, out var info) || info == null ||
            info.Permissions == null || !info.Permissions.Contains(TokenPermission.PlayGames))
        {
            throw new UnauthorizedAccessException("The Lichess OAuth token must include board:play.");
        }

        return info;
    }

    public async Task StartSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken = default)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        options.ToFormData();
        _logger.Information($"Starting seek: {options.TimeMinutes}+{options.IncrementSeconds}, " +
            $"rated={options.Rated}, color={options.Color}, variant={options.Variant}.");
        await StopAsync();
        SetState(LichessQueueState.ValidatingToken);

        try
        {
            await ValidatePlayTokenAsync(cancellationToken);
            _logger.Information("Token validated for board play.");
            cancellationToken.ThrowIfCancellationRequested();

            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _accountStreamErrorObserved = false;
            _accountStream = await _client.CreateBoardAccountEventStreamAsync(_cancellation.Token);
            _logger.Information("Account event stream created; attaching handlers.");
            _accountStream.LineReceived += HandleAccountLine;
            _accountStream.ErrorReceived += HandleStreamError;
            _accountStream.Completed += HandleStreamCompleted;

            // A deterministic fake may deliver the first event synchronously.
            SetState(LichessQueueState.Seeking);
            _accountStream.Start();
            _logger.Information("Waiting for account event request dispatch.");
            await _accountStream.Ready;
            _logger.Information("Account event request dispatched.");

            if (_state != LichessQueueState.Seeking)
            {
                _logger.Information("Game arrived before seek POST; skipping POST.");
                return;
            }

            _cancellation.Token.ThrowIfCancellationRequested();
            _logger.Information("Submitting seek POST; waiting for gameStart event.");
            _seekTask = RunSeekAsync(options, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Seek startup canceled.");
            await ReleaseResourcesAsync();
            SetState(LichessQueueState.Idle);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Seek startup failed: " + ex.Message);
            await ReleaseResourcesAsync();
            SetState(LichessQueueState.Faulted);
#pragma warning disable CA2200 // ExceptionDispatchInfo is not whitelisted by s&box.
            throw ex;
#pragma warning restore CA2200
        }
    }

    public async Task StopAsync()
    {
        if (_accountStream == null && _cancellation == null && _seekTask.IsCompleted)
        {
            SetState(LichessQueueState.Idle);
            return;
        }

        SetState(LichessQueueState.Stopping);
        await ReleaseResourcesAsync();
        SetState(LichessQueueState.Idle);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    private async Task RunSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken)
    {
        try
        {
            await _client.CreateBoardSeekAsync(options, cancellationToken);
            _logger.Information("Seek POST ended; account stream remains authoritative.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Information("Seek POST canceled.");
        }
        catch (Exception ex)
        {
            _logger.Error("Seek POST failed: " + ex.Message);
            OnError?.Invoke(this, ex);
            SetState(LichessQueueState.Faulted);
            if (_cancellation != null && !_cancellation.IsCancellationRequested)
                _cancellation.Cancel();
        }
    }

    private async Task ReleaseResourcesAsync()
    {
        var cancellation = _cancellation;
        var stream = _accountStream;
        var seekTask = _seekTask;

        _cancellation = null;
        _accountStream = null;
        _seekTask = Task.CompletedTask;

        if (cancellation != null && !cancellation.IsCancellationRequested)
            cancellation.Cancel();

        if (stream != null)
        {
            Unsubscribe(stream);
            await stream.DisposeAsync();
        }

        try
        {
            await seekTask;
        }
        catch (OperationCanceledException)
        {
        }

        cancellation?.Dispose();
    }

    private void HandleAccountLine(ILichessBoardEventStream stream, JsonElement data)
    {
        if (!ReferenceEquals(stream, _accountStream))
            return;

        try
        {
            var eventType = BoardEventParser.GetEventType(data);
            _logger.Debug("Account event received: " +
                (string.IsNullOrWhiteSpace(eventType) ? "[unknown]" : eventType) + ".");
            if (!eventType.Equals("gameStart", StringComparison.OrdinalIgnoreCase))
                return;

            var gameStart = BoardEventParser.ParseGameStart(data);
            var game = gameStart?.Game;
            var gameId = game?.BestGameId;
            if (string.IsNullOrWhiteSpace(gameId))
            {
                _logger.Warning("gameStart event did not contain a game id.");
                return;
            }

            _logger.Information($"Game accepted: id={gameId}, color={game?.Color ?? "unknown"}.");
            SetState(LichessQueueState.GameFound);
            OnGameFound?.Invoke(this, new LichessGameFoundEventArgs(gameId, game?.Color, game));

            if (StopWhenGameFound && _cancellation != null && !_cancellation.IsCancellationRequested)
                _cancellation.Cancel();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            SetState(LichessQueueState.Faulted);
        }
    }

    private void HandleStreamError(ILichessBoardEventStream stream, Exception exception)
    {
        if (!ReferenceEquals(stream, _accountStream))
            return;

        _accountStreamErrorObserved = true;
        _logger.Error("Account event stream failed: " + exception.Message);
        OnError?.Invoke(this, exception);
        SetState(LichessQueueState.Faulted);

        if (_cancellation != null && !_cancellation.IsCancellationRequested)
            _cancellation.Cancel();
    }

    private void HandleStreamCompleted(ILichessBoardEventStream stream)
    {
        if (!ReferenceEquals(stream, _accountStream) ||
            _state == LichessQueueState.Stopping || _cancellation == null)
        {
            return;
        }

        _logger.Information("Account event stream completed in state " + _state + ".");

        if (_state == LichessQueueState.GameFound)
        {
            _ = FinishCompletedStreamAsync(LichessQueueState.GameFound);
            return;
        }

        if (_cancellation.IsCancellationRequested)
        {
            var finalState = _state == LichessQueueState.Faulted
                ? LichessQueueState.Faulted
                : LichessQueueState.Idle;
            _ = FinishCompletedStreamAsync(finalState);
            return;
        }

        if (!_accountStreamErrorObserved)
            OnError?.Invoke(this, new IOException("The Lichess account event stream ended unexpectedly."));

        _cancellation.Cancel();
        _ = FinishCompletedStreamAsync(LichessQueueState.Faulted);
    }

    private async Task FinishCompletedStreamAsync(LichessQueueState finalState)
    {
        try
        {
            await ReleaseResourcesAsync();
            SetState(finalState);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            SetState(LichessQueueState.Faulted);
        }
    }

    private void Unsubscribe(ILichessBoardEventStream stream)
    {
        stream.LineReceived -= HandleAccountLine;
        stream.ErrorReceived -= HandleStreamError;
        stream.Completed -= HandleStreamCompleted;
    }

    private void SetState(LichessQueueState state)
    {
        if (_state == state)
            return;

        _state = state;
        _logger.Debug("State changed: " + state + ".");
        OnStateChanged?.Invoke(this, state);
    }
}
