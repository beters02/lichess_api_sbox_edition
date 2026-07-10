#nullable enable annotations

using LichessNET.API;
using LichessNET.Entities.Board;

namespace LichessNET.Gameplay;

public sealed class LichessGameSession : IAsyncDisposable
{
    private readonly LichessApiClient _api;
    private readonly IChessBoardAdapter _adapter;
    private readonly List<string> _moveHistory = new();
    private CancellationTokenSource _cancellation;
    private LichessNdjsonStream _stream;
    private bool _gameOverRaised;
    private string _lastConfirmedState;
    private string _pendingLocalMove;
    private string _pendingLocalSnapshot;

    public LichessGameSession(LichessApiClient api, string gameId, string myColor, IChessBoardAdapter adapter)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        GameId = string.IsNullOrWhiteSpace(gameId) ? throw new ArgumentException("Game id is required.", nameof(gameId)) : gameId;
        MyColor = string.IsNullOrWhiteSpace(myColor) ? "unknown" : myColor.ToLowerInvariant();
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _lastConfirmedState = SafeExportState();
    }

    public string GameId { get; }
    public string MyColor { get; }
    public IReadOnlyList<string> MoveHistory => _moveHistory;

    public event Action<LichessGameSession, string> OnOpponentMove;
    public event Action<LichessGameSession, BoardClockState> OnClockUpdate;
    public event Action<LichessGameSession, BoardGameState> OnGameOver;
    public event Action<LichessGameSession, BoardChatLineEvent> OnChatLine;
    public event Action<LichessGameSession, string> OnDesync;
    public event Action<LichessGameSession, Exception> OnError;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_stream != null)
            return;

        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _stream = await _api.StreamBoardGameAsync(GameId, _cancellation.Token);
        _stream.LineReceived += HandleStreamLine;
        _stream.ErrorReceived += HandleStreamError;
    }

    public async Task StopAsync()
    {
        if (_cancellation != null && !_cancellation.IsCancellationRequested)
            _cancellation.Cancel();

        if (_stream != null)
            await _stream.DisposeAsync();

        _stream = null;
        _cancellation?.Dispose();
        _cancellation = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public async Task<bool> SubmitLocalMoveAsync(string uci, bool offerDraw = false, CancellationToken cancellationToken = default)
    {
        if (!UciMove.TryParse(uci, out var move))
            throw new ArgumentException("Move must be valid UCI notation, for example e2e4 or e7e8q.", nameof(uci));

        if (_pendingLocalMove != null)
            throw new InvalidOperationException("A local move is already waiting for Lichess confirmation.");

        var normalizedMove = move.ToString();
        var snapshot = SafeExportState();
        if (!_adapter.TryApplyLocalMove(normalizedMove))
            return false;

        _pendingLocalMove = normalizedMove;
        _pendingLocalSnapshot = snapshot;

        try
        {
            var ok = await _api.MakeBoardMoveAsync(GameId, normalizedMove, offerDraw, cancellationToken);
            if (!ok)
            {
                RollBackPendingMove("Lichess rejected move " + normalizedMove + ".");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            RollBackPendingMove("Lichess move request failed for " + normalizedMove + ".");
            OnError?.Invoke(this, ex);
            return false;
        }
    }

    public Task<bool> ResignAsync(CancellationToken cancellationToken = default)
    {
        return _api.ResignBoardGameAsync(GameId, cancellationToken);
    }

    public Task<bool> AbortAsync(CancellationToken cancellationToken = default)
    {
        return _api.AbortBoardGameAsync(GameId, cancellationToken);
    }

    public Task<bool> OfferDrawAsync(CancellationToken cancellationToken = default)
    {
        return _api.HandleDrawOfferAsync(GameId, true, cancellationToken);
    }

    public Task<bool> AcceptDrawAsync(CancellationToken cancellationToken = default)
    {
        return _api.HandleDrawOfferAsync(GameId, true, cancellationToken);
    }

    public Task<bool> DeclineDrawAsync(CancellationToken cancellationToken = default)
    {
        return _api.HandleDrawOfferAsync(GameId, false, cancellationToken);
    }

    public Task<bool> SendChatAsync(string text, BoardChatRoom room = BoardChatRoom.Player,
        CancellationToken cancellationToken = default)
    {
        return _api.SendBoardChatAsync(GameId, text, room, cancellationToken);
    }

    private void HandleStreamLine(LichessNdjsonStream stream, JsonElement data)
    {
        try
        {
            var type = BoardEventParser.GetEventType(data);
            switch (type)
            {
                case "gameFull":
                    ApplyServerState(BoardEventParser.ParseGameFull(data)?.State);
                    break;
                case "gameState":
                    ApplyServerState(BoardEventParser.ParseGameState(data));
                    break;
                case "chatLine":
                    OnChatLine?.Invoke(this, BoardEventParser.ParseChatLine(data));
                    break;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private void ApplyServerState(BoardGameState state)
    {
        if (state == null)
            return;

        var serverMoves = state.MoveList;
        if (!IsCompatiblePrefix(serverMoves))
        {
            RestoreLastConfirmed("Server move list diverged from local history.");
            return;
        }

        for (var index = _moveHistory.Count; index < serverMoves.Count; index++)
        {
            var move = serverMoves[index];

            if (_pendingLocalMove != null)
            {
                if (string.Equals(move, _pendingLocalMove, StringComparison.OrdinalIgnoreCase))
                {
                    _moveHistory.Add(move);
                    _pendingLocalMove = null;
                    _pendingLocalSnapshot = null;
                    _lastConfirmedState = SafeExportState();
                    continue;
                }

                RollBackPendingMove("Server confirmed a different move than the pending local move.");
            }

            var beforeRemoteMove = SafeExportState();
            if (!_adapter.TryApplyRemoteMove(move))
            {
                RestoreSnapshot(beforeRemoteMove);
                OnDesync?.Invoke(this, "Board adapter rejected remote move " + move + ".");
                return;
            }

            _moveHistory.Add(move);
            _lastConfirmedState = SafeExportState();

            if (IsOpponentPly(index))
                OnOpponentMove?.Invoke(this, move);
        }

        OnClockUpdate?.Invoke(this, state.Clock);

        if (state.IsFinished && !_gameOverRaised)
        {
            _gameOverRaised = true;
            OnGameOver?.Invoke(this, state);
        }
    }

    private bool IsCompatiblePrefix(IReadOnlyList<string> serverMoves)
    {
        if (serverMoves.Count < _moveHistory.Count)
            return false;

        for (var i = 0; i < _moveHistory.Count; i++)
        {
            if (!string.Equals(serverMoves[i], _moveHistory[i], StringComparison.OrdinalIgnoreCase))
                return false;
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

        _pendingLocalMove = null;
        _pendingLocalSnapshot = null;
        OnDesync?.Invoke(this, reason);
    }

    private void RestoreLastConfirmed(string reason)
    {
        RestoreSnapshot(_lastConfirmedState);
        OnDesync?.Invoke(this, reason);
    }

    private string SafeExportState()
    {
        try
        {
            return _adapter.ExportState();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
            return string.Empty;
        }
    }

    private void RestoreSnapshot(string snapshot)
    {
        try
        {
            _adapter.ImportState(snapshot ?? string.Empty);
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
