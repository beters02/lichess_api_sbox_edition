#nullable enable annotations

using LichessNET.API;

namespace LichessNET.Entities.Game;

/// <summary>
/// Represents a stream for a Lichess game, handling real-time updates of moves and game information.
/// </summary>
public class GameStream
{
    public delegate void GameInfoFetchedHandler(object sender, OngoingGame game);
    public delegate void MoveUpdateHandler(object sender, Move move);

    private readonly Dictionary<string, OngoingGame> _games = new Dictionary<string, OngoingGame>();
    private readonly LichessStream _stream;

    public GameStream(string requestUri, string method = "GET")
        : this(requestUri, method, CancellationToken.None)
    {
    }

    public GameStream(string requestUri, string method, CancellationToken cancellationToken)
    {
        _stream = new LichessStream(requestUri, method);
        _stream.GameUpdateReceived += ProcessData;
        _ = _stream.StreamGameAsync(cancellationToken);
    }

    public event MoveUpdateHandler? OnMoveMade;
    public event GameInfoFetchedHandler? OnGameInfoFetched;

    private void ProcessData(object sender, JsonElement data)
    {
        FetchDataForNewGame(data);
        FetchDataForMove(data);
    }

    private void FetchDataForNewGame(JsonElement data)
    {
        if (!data.TryGetProperty("id", out var idProperty))
            return;

        var gameId = idProperty.GetString();
        if (string.IsNullOrWhiteSpace(gameId))
            return;

        var game = new OngoingGame
        {
            GameId = gameId,
            PlysAtInitFen = data.TryGetProperty("turns", out var turns) ? turns.GetInt32() : 0
        };

        _games[gameId] = game;
        OnGameInfoFetched?.Invoke(this, game);
    }

    private void FetchDataForMove(JsonElement data)
    {
        if (!data.TryGetProperty("lm", out var lastMoveProperty))
            return;

        var gameId = _games.Count == 1 ? _games.First().Key : string.Empty;
        if (string.IsNullOrWhiteSpace(gameId) || !_games.TryGetValue(gameId, out var game))
            return;

        game.Moves ??= new List<Move>();
        var fen = data.TryGetProperty("fen", out var fenProperty) ? fenProperty.GetString() : string.Empty;
        var move = new Move
        {
            Notation = lastMoveProperty.GetString(),
            IsWhite = fen?.Contains(" w ") ?? false,
            GameID = gameId,
            MoveNumber = (game.Moves.Count / 2) + 1
        };

        game.Moves.Add(move);
        OnMoveMade?.Invoke(this, move);
    }
}

