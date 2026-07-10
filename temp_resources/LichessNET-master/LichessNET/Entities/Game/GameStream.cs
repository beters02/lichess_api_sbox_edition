using LichessNET.API;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace LichessNET.Entities.Game;

/// <summary>
/// Represents a stream for a Lichess game, handling real-time updates of moves and game information.
/// Initializes a game stream and listens for game updates from the Lichess API.
/// </summary>
/// <example>
/// The following example shows how to create a game stream and listen for move updates.
/// <code>
/// async Task ExampleUsage()
/// {
///     var client = new LichessApiClient();
///     var gameStream = await client.GetGameStreamAsync("someGameId");
///     
///     // Add an event handler to write the last move to the console
///     gameStream.OnMoveMade += (sender, move) =>
///     {
///         Console.WriteLine($"Last move: {move.Notation}");
///     };
/// }
/// </code>
/// </example>
public class GameStream

{
    public delegate void GameInfoFetchedHandler(object sender, OngoingGame game);

    // Define a delegate for the event
    public delegate void MoveUpdateHandler(object sender, Move move);

    private Dictionary<string, OngoingGame> _games = new Dictionary<string, OngoingGame>();

    private LichessStream _stream;

    public GameStream(HttpRequestMessage request, HttpMethod method)
    {
        _stream = new LichessStream(request, method);
        _stream.GameUpdateReceived += ProcessData;
        Task.Run(() => _stream.StreamGameAsync());
    }

    public event MoveUpdateHandler? OnMoveMade;
    public event GameInfoFetchedHandler? OnGameInfoFetched;

    private void ProcessData(object o, JObject data)
    {
        //Check if it is data for a new game
        FetchDataForNewGame(data);
        //Check if it is data for a move
        FetchDataForMove(data);

        //Console.WriteLine(data);
    }

    private void FetchDataForNewGame(JObject data)
    {
        try
        {
            if (data.ContainsKey("id"))
            {
                var game = new OngoingGame();
                game.GameId = data["id"].ToObject<string>();
                game.PlysAtInitFen = data["turns"].ToObject<int>();
                //game.Fen = data["initialFen"].ToObject<string>();
                _games.Add(data["id"].ToObject<string>(), game);

                OnGameInfoFetched?.Invoke(this, game);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void FetchDataForMove(JObject data)
    {
        try
        {
            if (data.ContainsKey("lm"))
            {
                Console.WriteLine("Move made");

                var gameID = "";
                int moveNr = 1;
                if (_games.Count == 1)
                {
                    gameID = _games.First().Key;
                }

                if (_games[gameID].Moves == null) _games[gameID].Moves = new List<Move>();

                var move = new Move()
                {
                    Notation = data["lm"].ToString(),
                    IsWhite = data["fen"].ToString().Contains(" w "),
                    GameID = gameID,
                    MoveNumber = ((_games[gameID].Moves.Count()) / 2) + 1
                };

                if (_games.ContainsKey(gameID))
                {
                    _games[gameID].Moves.Add(move);
                }

                OnMoveMade?.Invoke(this, move);
            }
        }
        catch (Exception)
        {
            Console.WriteLine();
            throw;
        }
    }
}