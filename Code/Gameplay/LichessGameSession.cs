#nullable enable annotations

using LichessNET.API;
using LichessNET.Entities.Board;

namespace LichessNET.Gameplay;

public sealed class LichessGameSession : IAsyncDisposable
{
    private readonly ILichessBoardClient _client;
    private readonly IChessBoardAdapter _adapter;
    private readonly LichessGameSessionOptions _options;
    private readonly List<string> _moveHistory = new();
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly string _initialAdapterState;

    private CancellationTokenSource? _lifetimeCancellation;
    private ILichessBoardEventStream? _stream;
    private Task _reconnectTask = Task.CompletedTask;
    private bool _stopping;
    private bool _disposed;
    private bool _gameOverRaised;
    private bool _automaticReconnectRunning;
    private bool _automaticReconnectRequested;
    private bool _streamAuthenticationFailed;
    private bool _initialPositionReady;
    private int _automaticReconnectAttempt;
    private string? _appliedInitialFen;
    private string _authoritativeInitialState;
    private string _lastConfirmedState;
    private string? _pendingLocalMove;
    private string? _pendingLocalSnapshot;
    private LichessGameConnectionState _connectionState;

    public LichessGameSession(LichessApiClient api, string gameId, string myColor,
        IChessBoardAdapter adapter)
        : this((ILichessBoardClient)api, gameId, myColor, adapter, null)
    {
    }

    public LichessGameSession(LichessApiClient api, string gameId, string myColor,
        IChessBoardAdapter adapter, LichessGameSessionOptions? options)
        : this((ILichessBoardClient)api, gameId, myColor, adapter, options)
    {
    }

    public LichessGameSession(ILichessBoardClient client, string gameId, string myColor,
        IChessBoardAdapter adapter, LichessGameSessionOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        GameId = string.IsNullOrWhiteSpace(gameId)
            ? throw new ArgumentException("Game id is required.", nameof(gameId))
            : gameId;
        MyColor = string.IsNullOrWhiteSpace(myColor) ? "unknown" : myColor.ToLowerInvariant();
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _options = options ?? new LichessGameSessionOptions();

        _initialAdapterState = SafeExportState();
        _authoritativeInitialState = _initialAdapterState;
        _lastConfirmedState = _initialAdapterState;
    }

    public ILichessBoardClient BoardClient => _client;
    public string GameId { get; }
    public string MyColor { get; }
    public IReadOnlyList<string> MoveHistory => _moveHistory;
    public LichessGameSessionOptions Options => _options;
    public BoardGameFullEvent? GameFull { get; private set; }
    public BoardGameState? LatestState { get; private set; }
    public string? PendingLocalMove => _pendingLocalMove;
    public bool WhiteOfferingDraw => LatestState?.WhiteOfferingDraw == true;
    public bool BlackOfferingDraw => LatestState?.BlackOfferingDraw == true;
    public LichessGameConnectionState ConnectionState => _connectionState;
    public bool IsConnected => _connectionState == LichessGameConnectionState.Connected;

    public event Action<LichessGameSession, string>? OnOpponentMove;
    public event Action<LichessGameSession, BoardClockState>? OnClockUpdate;
    public event Action<LichessGameSession, BoardGameState>? OnGameOver;
    public event Action<LichessGameSession, BoardChatLineEvent>? OnChatLine;
    public event Action<LichessGameSession, string>? OnDesync;
    public event Action<LichessGameSession, Exception>? OnError;
    public event Action<LichessGameSession, BoardGameFullEvent>? OnGameFull;
    public event Action<LichessGameSession, BoardGameState>? OnStateUpdated;
    public event Action<LichessGameSession, bool, bool>? OnDrawOfferChanged;
    public event Action<LichessGameSession, string?>? OnPendingLocalMoveChanged;
    public event Action<LichessGameSession, LichessGameConnectionState>? OnConnectionStateChanged;
    public event Action<LichessGameSession>? OnUnexpectedCompletion;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_stream != null)
            return;

        _stopping = false;
        if (_lifetimeCancellation == null || _lifetimeCancellation.IsCancellationRequested)
        {
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_stream == null)
                await OpenStreamAsync(LichessGameConnectionState.Connecting, cancellationToken);
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (LatestState?.IsFinished == true)
            return;

        _stopping = false;
        if (_lifetimeCancellation == null || _lifetimeCancellation.IsCancellationRequested)
        {
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            _lifetimeCancellation.Token, cancellationToken);

        try
        {
            await ReconnectCoreAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    public async Task StopAsync()
    {
        _stopping = true;
        var lifetime = _lifetimeCancellation;
        if (lifetime != null && !lifetime.IsCancellationRequested)
            lifetime.Cancel();

        await _connectionGate.WaitAsync();
        try
        {
            await CloseCurrentStreamAsync();
        }
        finally
        {
            _connectionGate.Release();
        }

        var reconnectTask = _reconnectTask;
        try
        {
            await reconnectTask;
        }
        catch (OperationCanceledException)
        {
        }

        if (ReferenceEquals(_lifetimeCancellation, lifetime))
            _lifetimeCancellation = null;

        lifetime?.Dispose();

        if (LatestState?.IsFinished == true)
            SetConnectionState(LichessGameConnectionState.Finished);
        else
            SetConnectionState(LichessGameConnectionState.Disconnected);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await StopAsync();
    }

    public async Task<bool> SubmitLocalMoveAsync(string uci, bool offerDraw = false,
        CancellationToken cancellationToken = default)
    {
        if (!UciMove.TryParse(uci, out var move))
        {
            throw new ArgumentException(
                "Move must be valid UCI notation, for example e2e4 or e7e8q.", nameof(uci));
        }

        if (_pendingLocalMove != null)
            throw new InvalidOperationException("A local move is already waiting for Lichess confirmation.");

        var normalizedMove = move.ToString();
        var snapshot = SafeExportState();
        if (!_adapter.TryApplyLocalMove(normalizedMove))
            return false;

        SetPendingLocalMove(normalizedMove, snapshot);

        try
        {
            var ok = await _client.MakeBoardMoveAsync(
                GameId, normalizedMove, offerDraw, cancellationToken);

            if (!ok && string.Equals(_pendingLocalMove, normalizedMove,
                    StringComparison.OrdinalIgnoreCase))
            {
                RollBackPendingMove("Lichess rejected move " + normalizedMove + ".");
            }

            return ok;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (string.Equals(_pendingLocalMove, normalizedMove, StringComparison.OrdinalIgnoreCase))
                RollBackPendingMove("The pending move was canceled.");

            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            if (string.Equals(_pendingLocalMove, normalizedMove, StringComparison.OrdinalIgnoreCase))
            {
                RollBackPendingMove(
                    "Lichess move request failed for " + normalizedMove + ".");
            }

            OnError?.Invoke(this, ex);
            return false;
        }
    }

    public Task<bool> ResignAsync(CancellationToken cancellationToken = default)
    {
        return _client.ResignBoardGameAsync(GameId, cancellationToken);
    }

    public Task<bool> AbortAsync(CancellationToken cancellationToken = default)
    {
        return _client.AbortBoardGameAsync(GameId, cancellationToken);
    }

    public Task<bool> OfferDrawAsync(CancellationToken cancellationToken = default)
    {
        return _client.HandleDrawOfferAsync(GameId, true, cancellationToken);
    }

    public Task<bool> AcceptDrawAsync(CancellationToken cancellationToken = default)
    {
        return _client.HandleDrawOfferAsync(GameId, true, cancellationToken);
    }

    public Task<bool> DeclineDrawAsync(CancellationToken cancellationToken = default)
    {
        return _client.HandleDrawOfferAsync(GameId, false, cancellationToken);
    }

    public Task<bool> SendChatAsync(string text, BoardChatRoom room = BoardChatRoom.Player,
        CancellationToken cancellationToken = default)
    {
        return _client.SendBoardChatAsync(GameId, text, room, cancellationToken);
    }

    private async Task<bool> ReconnectCoreAsync(CancellationToken cancellationToken)
    {
        await _connectionGate.WaitAsync(cancellationToken);
        try
        {
            if (_stopping || cancellationToken.IsCancellationRequested ||
                LatestState?.IsFinished == true)
            {
                return false;
            }

            await CloseCurrentStreamAsync();
            return await OpenStreamAsync(
                LichessGameConnectionState.Reconnecting, cancellationToken);
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    private async Task<bool> OpenStreamAsync(LichessGameConnectionState openingState,
        CancellationToken cancellationToken)
    {
        SetConnectionState(openingState);

        ILichessBoardEventStream? stream = null;
        try
        {
            stream = await _client.CreateBoardGameStreamAsync(GameId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (_stopping)
            {
                await DisposeUnstartedStreamAsync(stream);
                SetConnectionState(LichessGameConnectionState.Disconnected);
                return false;
            }

            _streamAuthenticationFailed = false;
            _stream = stream;
            stream.LineReceived += HandleStreamLine;
            stream.ErrorReceived += HandleStreamError;
            stream.Completed += HandleStreamCompleted;
            stream.Start();

            if (!ReferenceEquals(_stream, stream))
                return false;

            if (LatestState?.IsFinished == true)
                SetConnectionState(LichessGameConnectionState.Finished);
            else
                SetConnectionState(LichessGameConnectionState.Connected);

            return true;
        }
        catch (OperationCanceledException)
        {
            await DisposeUnstartedStreamAsync(stream);
            SetConnectionState(LichessGameConnectionState.Disconnected);
            throw new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            await DisposeUnstartedStreamAsync(stream);
            SetConnectionState(LichessGameConnectionState.Disconnected);
#pragma warning disable CA2200 // ExceptionDispatchInfo is not whitelisted by s&box.
            throw ex;
#pragma warning restore CA2200
        }
    }

    private async Task DisposeUnstartedStreamAsync(ILichessBoardEventStream? stream)
    {
        if (stream == null)
            return;

        if (ReferenceEquals(_stream, stream))
            _stream = null;

        Unsubscribe(stream);
        await stream.DisposeAsync();
    }

    private async Task CloseCurrentStreamAsync()
    {
        var stream = _stream;
        _stream = null;
        if (stream == null)
            return;

        Unsubscribe(stream);
        await stream.DisposeAsync();
    }

    private void HandleStreamLine(ILichessBoardEventStream stream, JsonElement data)
    {
        if (!ReferenceEquals(stream, _stream))
            return;

        _automaticReconnectAttempt = 0;
        SetConnectionState(LichessGameConnectionState.Connected);

        try
        {
            var type = BoardEventParser.GetEventType(data);
            switch (type)
            {
                case "gameFull":
                {
                    var full = BoardEventParser.ParseGameFull(data);
                    if (full == null)
                        return;

                    GameFull = full;
                    if (PrepareInitialPosition(full.InitialFen))
                        ApplyServerState(full.State, true);

                    OnGameFull?.Invoke(this, full);
                    break;
                }
                case "gameState":
                    if (_initialPositionReady)
                        ApplyServerState(BoardEventParser.ParseGameState(data), false);
                    break;
                case "chatLine":
                {
                    var chat = BoardEventParser.ParseChatLine(data);
                    if (chat != null)
                        OnChatLine?.Invoke(this, chat);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private void HandleStreamError(ILichessBoardEventStream stream, Exception exception)
    {
        if (!ReferenceEquals(stream, _stream))
            return;

        _streamAuthenticationFailed = IsAuthenticationFailure(exception);
        if (_streamAuthenticationFailed)
        {
            if (_lifetimeCancellation != null &&
                !_lifetimeCancellation.IsCancellationRequested)
            {
                _lifetimeCancellation.Cancel();
            }

            SetConnectionState(LichessGameConnectionState.Disconnected);
        }

        OnError?.Invoke(this, exception);
    }

    private void HandleStreamCompleted(ILichessBoardEventStream stream)
    {
        if (!ReferenceEquals(stream, _stream))
            return;

        _stream = null;
        Unsubscribe(stream);
        _ = DisposeCompletedStreamAsync(stream);

        var authenticationFailure = _streamAuthenticationFailed;
        _streamAuthenticationFailed = false;

        if (LatestState?.IsFinished == true)
        {
            SetConnectionState(LichessGameConnectionState.Finished);
            return;
        }

        if (_stopping || _lifetimeCancellation == null)
        {
            SetConnectionState(LichessGameConnectionState.Disconnected);
            return;
        }

        if (authenticationFailure)
        {
            _automaticReconnectRequested = false;
            SetConnectionState(LichessGameConnectionState.Disconnected);
            OnUnexpectedCompletion?.Invoke(this);
            return;
        }

        if (_lifetimeCancellation.IsCancellationRequested)
        {
            SetConnectionState(LichessGameConnectionState.Disconnected);
            return;
        }

        SetConnectionState(LichessGameConnectionState.Disconnected);
        if (_options.AutoReconnect)
            RequestAutomaticReconnect(_lifetimeCancellation.Token);

        OnUnexpectedCompletion?.Invoke(this);
    }

    private void RequestAutomaticReconnect(CancellationToken cancellationToken)
    {
        _automaticReconnectRequested = true;
        if (_automaticReconnectRunning)
            return;

        _automaticReconnectRequested = false;
        _automaticReconnectRunning = true;
        _reconnectTask = RunAutomaticReconnectAsync(cancellationToken);
    }

    private async Task DisposeCompletedStreamAsync(ILichessBoardEventStream stream)
    {
        try
        {
            await stream.DisposeAsync();
        }
        catch (Exception ex)
        {
            if (!_stopping)
                OnError?.Invoke(this, ex);
        }
    }

    private async Task RunAutomaticReconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !_stopping &&
                   LatestState?.IsFinished != true)
            {
                TimeSpan delay;
                try
                {
                    delay = _options.GetReconnectDelay(_automaticReconnectAttempt);
                    _automaticReconnectAttempt++;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, ex);
                    return;
                }

                SetConnectionState(LichessGameConnectionState.Reconnecting);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                    if (await ReconnectCoreAsync(cancellationToken))
                        return;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    SetConnectionState(LichessGameConnectionState.Disconnected);
                    OnError?.Invoke(this, ex);

                    if (IsAuthenticationFailure(ex))
                    {
                        _automaticReconnectRequested = false;
                        if (_lifetimeCancellation != null &&
                            !_lifetimeCancellation.IsCancellationRequested)
                        {
                            _lifetimeCancellation.Cancel();
                        }

                        return;
                    }
                }
            }
        }
        finally
        {
            _automaticReconnectRunning = false;
            if (_automaticReconnectRequested &&
                !cancellationToken.IsCancellationRequested && !_stopping &&
                LatestState?.IsFinished != true)
            {
                RequestAutomaticReconnect(cancellationToken);
            }
            else if (cancellationToken.IsCancellationRequested || _stopping ||
                     LatestState?.IsFinished == true)
            {
                _automaticReconnectRequested = false;
            }
        }
    }

    private bool PrepareInitialPosition(string? initialFen)
    {
        var normalizedFen = string.IsNullOrWhiteSpace(initialFen)
            ? "startpos"
            : initialFen.Trim();

        _initialPositionReady = false;
        if (string.Equals(_appliedInitialFen, normalizedFen,
                StringComparison.OrdinalIgnoreCase))
        {
            _initialPositionReady = true;
            return true;
        }

        if (!RestoreSnapshot(_initialAdapterState))
            return false;

        if (!normalizedFen.Equals("startpos", StringComparison.OrdinalIgnoreCase))
        {
            if (_adapter is not IChessInitialPositionAdapter initialPositionAdapter ||
                !initialPositionAdapter.TrySetInitialPosition(normalizedFen))
            {
                RestoreSnapshot(_initialAdapterState);
                OnDesync?.Invoke(this,
                    "The board adapter does not support the game's non-starting FEN.");
                return false;
            }
        }

        _appliedInitialFen = normalizedFen;
        _authoritativeInitialState = SafeExportState();
        _lastConfirmedState = _authoritativeInitialState;
        _moveHistory.Clear();
        SetPendingLocalMove(null, null);
        _initialPositionReady = true;
        return true;
    }

    private void ApplyServerState(BoardGameState? state, bool fromGameFull)
    {
        if (state == null)
            return;

        var oldWhiteDraw = WhiteOfferingDraw;
        var oldBlackDraw = BlackOfferingDraw;
        LatestState = state;

        var serverMoves = state.MoveList;
        if (!IsCompatiblePrefix(serverMoves))
        {
            RebuildAuthoritativeHistory(
                serverMoves, "Server move list diverged from confirmed local history.");
        }
        else
        {
            ApplyNewServerMoves(serverMoves, fromGameFull);
        }

        OnClockUpdate?.Invoke(this, state.Clock);
        OnStateUpdated?.Invoke(this, state);

        if (oldWhiteDraw != WhiteOfferingDraw || oldBlackDraw != BlackOfferingDraw)
        {
            OnDrawOfferChanged?.Invoke(
                this, WhiteOfferingDraw, BlackOfferingDraw);
        }

        if (state.IsFinished)
        {
            SetConnectionState(LichessGameConnectionState.Finished);
            if (_lifetimeCancellation != null &&
                !_lifetimeCancellation.IsCancellationRequested)
            {
                _lifetimeCancellation.Cancel();
            }

            if (!_gameOverRaised)
            {
                _gameOverRaised = true;
                OnGameOver?.Invoke(this, state);
            }
        }
    }

    private void ApplyNewServerMoves(IReadOnlyList<string> serverMoves, bool fromGameFull)
    {
        for (var index = _moveHistory.Count; index < serverMoves.Count; index++)
        {
            var move = serverMoves[index];

            if (_pendingLocalMove != null)
            {
                if (string.Equals(move, _pendingLocalMove,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _moveHistory.Add(move);
                    SetPendingLocalMove(null, null);
                    _lastConfirmedState = SafeExportState();
                    continue;
                }

                RollBackPendingMove(
                    "Server confirmed a different move than the pending local move.");
            }

            var beforeRemoteMove = SafeExportState();
            if (!_adapter.TryApplyRemoteMove(move))
            {
                RestoreSnapshot(beforeRemoteMove);
                OnDesync?.Invoke(this,
                    "Board adapter rejected remote move " + move + ".");
                return;
            }

            _moveHistory.Add(move);
            _lastConfirmedState = SafeExportState();

            if (!fromGameFull && IsOpponentPly(index))
                OnOpponentMove?.Invoke(this, move);
        }
    }

    private bool RebuildAuthoritativeHistory(IReadOnlyList<string> serverMoves, string reason)
    {
        SetPendingLocalMove(null, null);
        if (!RestoreSnapshot(_authoritativeInitialState))
            return false;

        _moveHistory.Clear();
        _lastConfirmedState = _authoritativeInitialState;

        foreach (var move in serverMoves)
        {
            var beforeMove = SafeExportState();
            if (!_adapter.TryApplyRemoteMove(move))
            {
                RestoreSnapshot(beforeMove);
                _lastConfirmedState = SafeExportState();
                OnDesync?.Invoke(this,
                    reason + " Board adapter rejected authoritative move " + move + ".");
                return false;
            }

            _moveHistory.Add(move);
            _lastConfirmedState = SafeExportState();
        }

        OnDesync?.Invoke(this, reason + " Rebuilt from the authoritative history.");
        return true;
    }

    private bool IsCompatiblePrefix(IReadOnlyList<string> serverMoves)
    {
        if (serverMoves.Count < _moveHistory.Count)
            return false;

        for (var i = 0; i < _moveHistory.Count; i++)
        {
            if (!string.Equals(serverMoves[i], _moveHistory[i],
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsOpponentPly(int plyIndex)
    {
        return MyColor switch
        {
            "white" => plyIndex % 2 == 1,
            "black" => plyIndex % 2 == 0,
            _ => true
        };
    }

    private void RollBackPendingMove(string reason)
    {
        if (_pendingLocalSnapshot != null)
            RestoreSnapshot(_pendingLocalSnapshot);

        SetPendingLocalMove(null, null);
        OnDesync?.Invoke(this, reason);
    }

    private void SetPendingLocalMove(string? move, string? snapshot)
    {
        var changed = !string.Equals(
            _pendingLocalMove, move, StringComparison.OrdinalIgnoreCase);
        _pendingLocalMove = move;
        _pendingLocalSnapshot = snapshot;

        if (changed)
            OnPendingLocalMoveChanged?.Invoke(this, move);
    }

    private string SafeExportState()
    {
        try
        {
            return _adapter.ExportState() ?? string.Empty;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return string.Empty;
        }
    }

    private bool RestoreSnapshot(string? snapshot)
    {
        try
        {
            _adapter.ImportState(snapshot ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return false;
        }
    }

    private void SetConnectionState(LichessGameConnectionState state)
    {
        if (_connectionState == state)
            return;

        _connectionState = state;
        OnConnectionStateChanged?.Invoke(this, state);
    }

    private void Unsubscribe(ILichessBoardEventStream stream)
    {
        stream.LineReceived -= HandleStreamLine;
        stream.ErrorReceived -= HandleStreamError;
        stream.Completed -= HandleStreamCompleted;
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        if (exception is not HttpRequestException httpException)
            return false;

        return httpException.StatusCode == HttpStatusCode.Unauthorized ||
               httpException.StatusCode == HttpStatusCode.Forbidden;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LichessGameSession));
    }
}
