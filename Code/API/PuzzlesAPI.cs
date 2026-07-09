using LichessNET.Entities.Puzzle;
using LichessNET.Entities.Puzzle.Dashboard;
using LichessNET.Entities.Puzzle.PuzzleStorm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Retrieves the daily puzzle from the Lichess API.
    /// </summary>
    /// <returns>
    /// A <see cref="Puzzle"/> object representing the daily puzzle, including associated game data and solution themes.
    /// </returns>
    public async Task<Puzzle> GetDailyPuzzle()
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/puzzle/daily");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var jobj = JsonConvert.DeserializeObject<JObject>(content);
        var puzzle = jobj["puzzle"].ToObject<Puzzle>();
        puzzle.Game = jobj["game"].ToObject<PuzzleGame>();
        return puzzle;
    }

    /// <summary>
    /// Fetches a random chess puzzle from the Lichess API.
    /// </summary>
    /// <returns>
    /// A <see cref="Puzzle"/> object representing the random chess puzzle, including its associated game details.
    /// </returns>
    public async Task<Puzzle> GetRandomPuzzle()
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/puzzle/next");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var jobj = JsonConvert.DeserializeObject<JObject>(content);
        var puzzle = jobj["puzzle"].ToObject<Puzzle>();
        puzzle.Game = jobj["game"].ToObject<PuzzleGame>();
        return puzzle;
    }

    /// <summary>
    /// Retrieves a specific chess puzzle from the Lichess API using its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the puzzle to retrieve.</param>
    /// <returns>A <see cref="Puzzle"/> object containing the details of the requested puzzle, including its game data and solution themes.</returns>
    public async Task<Puzzle> GetPuzzleByID(string id)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/puzzle/{id}");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var jobj = JsonConvert.DeserializeObject<JObject>(content);
        var puzzle = jobj["puzzle"].ToObject<Puzzle>();
        puzzle.Game = jobj["game"].ToObject<PuzzleGame>();
        return puzzle;
    }

    /// <summary>
    /// Retrieves the puzzle dashboard for a specified number of days.
    /// </summary>
    /// <param name="days">The number of days to look back when aggregating puzzle results.</param>
    /// <returns>A task representing the asynchronous operation, containing the puzzle dashboard data.</returns>
    public async Task<PuzzleDashboard> GetPuzzleDashboardAsync(int days)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/puzzle/dashboard/{days}";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var puzzleDashboardResponse = JsonConvert.DeserializeObject<PuzzleDashboard>(content);

        return puzzleDashboardResponse;
    }

    /// <summary>
    /// Retrieves the storm dashboard for a specified username and number of days.
    /// </summary>
    /// <param name="username">The username of the player.</param>
    /// <param name="days">The number of days of history to return. Default is 30.</param>
    /// <returns>A task representing the asynchronous operation, containing the storm dashboard data.</returns>
    public async Task<StormDashboard> GetStormDashboardAsync(string username, int days = 30)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/storm/dashboard/{username}";
        var request = GetRequestScaffold(endpoint, new Tuple<string, string>("days", days.ToString()));

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var stormDashboardResponse = JsonConvert.DeserializeObject<StormDashboard>(content);

        return stormDashboardResponse;
    }


    /// <summary>
    /// Creates a new private puzzle race. It will have to be started manually after creating.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the puzzle race data.</returns>
    public async Task<PuzzleRace> CreatePuzzleRaceAsync()
    {
        _ratelimitController.Consume();

        var endpoint = "api/racer";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();

        var puzzleRaceResponse = JsonConvert.DeserializeObject<PuzzleRace>(content);

        return puzzleRaceResponse;
    }
}