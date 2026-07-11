#nullable enable annotations

using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.OAuth;

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
        await StopAsync();
        SetState(LichessQueueState.ValidatingToken);

        try
        {
            await ValidatePlayTokenAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _accountStreamErrorObserved = false;
            _accountStream = await _client.CreateBoardAccountEventStreamAsync(_cancellation.Token);
            _accountStream.LineReceived += HandleAccountLine;
            _accountStream.ErrorReceived += HandleStreamError;
            _accountStream.Completed += HandleStreamCompleted;

            // A deterministic fake may deliver the first event synchronously.
            SetState(LichessQueueState.Seeking);
            _accountStream.Start();
            _seekTask = RunSeekAsync(options, _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await ReleaseResourcesAsync();
            SetState(LichessQueueState.Idle);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
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
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
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
            if (!BoardEventParser.GetEventType(data).Equals("gameStart", StringComparison.OrdinalIgnoreCase))
                return;

            var gameStart = BoardEventParser.ParseGameStart(data);
            var game = gameStart?.Game;
            var gameId = game?.BestGameId;
            if (string.IsNullOrWhiteSpace(gameId))
                return;

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
        OnStateChanged?.Invoke(this, state);
    }
}
