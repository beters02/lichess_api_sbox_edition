#nullable enable annotations

using LichessNET.Entities.Puzzle;
using LichessNET.Entities.Puzzle.Dashboard;
using LichessNET.Entities.Puzzle.PuzzleStorm;

namespace LichessNET.API;

public partial class LichessApiClient
{
    public async Task<Puzzle> GetDailyPuzzle()
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold("api/puzzle/daily");
        Log.Info(request);
        var response = await SendRequest(request);
        return await ReadPuzzleResponse(response);
    }

    public async Task<Puzzle> GetRandomPuzzle()
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold("api/puzzle/next");
        var response = await SendRequest(request);
        return await ReadPuzzleResponse(response);
    }

    public async Task<Puzzle> GetPuzzleByID(string id)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/puzzle/{id}");
        var response = await SendRequest(request);
        return await ReadPuzzleResponse(response);
    }

    public async Task<PuzzleDashboard> GetPuzzleDashboardAsync(int days)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/puzzle/dashboard/{days}");
        var response = await SendRequest(request);
        return LichessJson.Deserialize<PuzzleDashboard>(await response.Content.ReadAsStringAsync());
    }

    public async Task<StormDashboard> GetStormDashboardAsync(string username, int days = 30)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/storm/dashboard/{username}", new Tuple<string, string>("days", days.ToString()));
        var response = await SendRequest(request);
        return LichessJson.Deserialize<StormDashboard>(await response.Content.ReadAsStringAsync());
    }

    public async Task<PuzzleRace> CreatePuzzleRaceAsync()
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold("api/racer");
        var response = await SendRequest(request, "POST");
        return LichessJson.Deserialize<PuzzleRace>(await response.Content.ReadAsStringAsync());
    }

    private static async Task<Puzzle> ReadPuzzleResponse(LichessResponse response)
    {
        var json = LichessJson.Parse(await response.Content.ReadAsStringAsync());
        var puzzle = json.GetProperty("puzzle").Deserialize<Puzzle>(LichessJson.Options);
        puzzle.Game = json.GetProperty("game").Deserialize<PuzzleGame>(LichessJson.Options);
        return puzzle;
    }
}



