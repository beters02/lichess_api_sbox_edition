#nullable enable annotations

using System.Text;
using LichessNET.Entities.Game;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Retrieves a chess game using its unique identifier from the Lichess API.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to retrieve.</param>
    public Task<Game> GetGameAsync(string gameId)
    {
        return GetGameAsync(gameId, new GameExportOptions(), CancellationToken.None);
    }

    /// <summary>
    /// Retrieves and parses one game while observing cancellation.
    /// </summary>
    public Task<Game> GetGameAsync(string gameId, CancellationToken cancellationToken)
    {
        return GetGameAsync(gameId, new GameExportOptions(), cancellationToken);
    }

    /// <summary>
    /// Retrieves and parses one game using the supplied export options.
    /// </summary>
    public Task<Game> GetGameAsync(string gameId, GameExportOptions options)
    {
        return GetGameAsync(gameId, options, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves and parses one game using the supplied export options.
    /// </summary>
    public async Task<Game> GetGameAsync(string gameId, GameExportOptions options,
        CancellationToken cancellationToken)
    {
        var pgn = await GetGamePgnAsync(gameId, options, cancellationToken);
        return Game.FromPgn(pgn);
    }

    /// <summary>
    /// Retrieves the exact PGN returned by Lichess without parsing or trimming it.
    /// </summary>
    public Task<string> GetGamePgnAsync(string gameId)
    {
        return GetGamePgnAsync(gameId, new GameExportOptions(), CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the exact PGN returned by Lichess while observing cancellation.
    /// </summary>
    public Task<string> GetGamePgnAsync(string gameId, CancellationToken cancellationToken)
    {
        return GetGamePgnAsync(gameId, new GameExportOptions(), cancellationToken);
    }

    /// <summary>
    /// Retrieves the exact PGN returned by Lichess using the supplied options.
    /// </summary>
    public Task<string> GetGamePgnAsync(string gameId, GameExportOptions options)
    {
        return GetGamePgnAsync(gameId, options, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the exact PGN returned by Lichess using the supplied options.
    /// </summary>
    public async Task<string> GetGamePgnAsync(string gameId, GameExportOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game ID cannot be empty.", nameof(gameId));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        cancellationToken.ThrowIfCancellationRequested();

        var request = GetRequestScaffold(
            "game/export/" + Uri.EscapeDataString(gameId.Trim()),
            options.ToQueryParameters());
        request.Headers["Accept"] = "application/x-chess-pgn";

        var response = await SendRequest(request, cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return content;
    }

    /// <summary>
    /// Retrieves a list of chess games for a specified user from the Lichess API.
    /// </summary>
    /// <param name="username">The username of the player whose games are to be retrieved.</param>
    /// <param name="max">The maximum number of games to retrieve. Default is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of games.</returns>
    public async Task<List<Game>> GetGamesAsync(string username, int max = 10)
    {
        await _ratelimitController.Consume();

        var request = GetRequestScaffold("api/games/user/" + username,
            Tuple.Create("max", max.ToString()));

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return ParsePgnGames(content);
    }

    /// <summary>
    /// Retrieves multiple chess games from the Lichess API using a list of unique identifiers.
    /// </summary>
    /// <param name="ids">An array of unique game identifiers to retrieve.</param>
    /// <returns>A list of <see cref="Game"/> objects representing the retrieved chess games.</returns>
    public async Task<List<Game>> GetGamesAsync(params string[] ids)
    {
        var request = GetRequestScaffold("api/games/export/_ids");
        request.Query["ids"] = string.Join(",", ids);
        var response = await SendRequest(request, "POST");
        var content = await response.Content.ReadAsStringAsync();

        return ParsePgnGames(content);
    }

    /// <summary>
    /// Retrieves a list of chess games that have been imported to the Lichess platform.
    /// </summary>
    /// <returns>A list of imported chess games.</returns>
    public async Task<List<Game>> GetImportedGamesAsync()
    {
        await _ratelimitController.Consume();

        var request = GetRequestScaffold("api/games/export/import");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return ParsePgnGames(content);
    }

    /// <summary>
    /// Retrieves a list of chess games from a specified arena using the provided arena identifier.
    /// </summary>
    /// <param name="ArenaID">The unique identifier of the arena from which to retrieve the games.</param>
    /// <returns>A task representing the asynchronous operation that returns a list of games retrieved from the specified arena.</returns>
    public async Task<List<Game>> GetArenaGames(string ArenaID)
    {
        await _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/tournament/{ArenaID}/games");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return ParsePgnGames(content);
    }

    /// <summary>
    /// Retrieves a list of games from a Swiss tournament using the Swiss ID from the Lichess API.
    /// </summary>
    /// <param name="SwissID">The unique identifier of the Swiss tournament to retrieve games from.</param>
    /// <returns>A task representing the asynchronous operation, with a list of Swiss tournament games as the result.</returns>
    public async Task<List<Game>> GetSwissGames(string SwissID)
    {
        await _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/swiss/{SwissID}/games");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return ParsePgnGames(content);
    }

    private static List<Game> ParsePgnGames(string content)
    {
        var games = new List<Game>();
        var gamePgns = System.Text.RegularExpressions.Regex.Split(
            content ?? string.Empty,
            @"(?m)(?=^[\t ]*\[Event\s+"")",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        foreach (var gamePgn in gamePgns)
        {
            if (string.IsNullOrWhiteSpace(gamePgn))
                continue;

            games.Add(Game.FromPgn(gamePgn.Trim('\r', '\n')));
        }

        return games;
    }

    /// <summary>
    /// Initializes a real-time stream of a chess game using its unique identifier from the Lichess API.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to stream.</param>
    /// <returns>A GameStream object that provides updates as the game progresses.</returns>
    public async Task<GameStream> GetGameStreamAsync(string gameId)
    {
        return new GameStream("https://lichess.org/api/stream/game/" + Uri.EscapeDataString(gameId), "POST");
    }

    /// <summary>
    /// Stream the games played between a list of users, in real time.
    /// Only games where both players are part of the list are included.
    /// The stream emits an event each time a game is started or finished.
    /// To also get all current ongoing games at the beginning of the stream, use the withCurrentGames flag. 
    /// </summary>
    /// <param name="UserIDs"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<GameStream> GetGameStreamByUserAsync(params string[] UserIDs)
    {
        if (UserIDs.Length > 300) throw new Exception("Lichess only allows up to 300 users to be tracked at once.");
        var users = Uri.EscapeDataString(string.Join(",", UserIDs));
        return new GameStream("https://lichess.org/api/stream/games-by-users?ids=" + users, "POST");
    }

    public async Task<GameStream> GetGameStreamByIDsAsync(params string[] GameIDs)
    {
        if (GameIDs.Length > 500) throw new Exception("Lichess only allows up to 500 games to be tracked at once.");
        var games = Uri.EscapeDataString(string.Join(",", GameIDs));
        return new GameStream("https://lichess.org/api/stream/games/" + games, "POST");
    }

    /// <summary>
    /// Fetches the ongoing games for the current user.
    /// </summary>
    /// <param name="maxGames">The maximum number of ongoing games to fetch (default is 9, max 50).</param>
    /// <returns>A list of OngoingGame objects representing the current ongoing games.</returns>
    public Task<List<OngoingGame>> GetOngoingGamesAsync(int maxGames = 9)
    {
        return GetOngoingGamesAsync(maxGames, CancellationToken.None);
    }

    /// <summary>
    /// Fetches the ongoing games for the current user and observes cancellation.
    /// </summary>
    public async Task<List<OngoingGame>> GetOngoingGamesAsync(int maxGames,
        CancellationToken cancellationToken)
    {
        if (maxGames < 1 || maxGames > 50)
            throw new ArgumentOutOfRangeException(nameof(maxGames),
                "The number of games must be between 1 and 50.");

        await _ratelimitController.Consume("api/account", false, cancellationToken);

        var request = GetRequestScaffold("api/account/playing",
            Tuple.Create("nb", maxGames.ToString()));
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var json = LichessJson.Parse(content);
        return json.TryGetProperty("nowPlaying", out var nowPlaying)
            ? nowPlaying.Deserialize<List<OngoingGame>>(LichessJson.Options)
                ?? new List<OngoingGame>()
            : new List<OngoingGame>();
    }
}




