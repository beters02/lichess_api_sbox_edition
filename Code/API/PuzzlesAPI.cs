#nullable enable annotations

using LichessNET.Entities.Puzzle;
using System.Text;
using LichessNET.Entities.Puzzle.Dashboard;
using LichessNET.Entities.Puzzle.PuzzleStorm;

namespace LichessNET.API;

public partial class LichessApiClient
{
    public Task<Puzzle> GetDailyPuzzle()
    {
        return GetDailyPuzzle(CancellationToken.None);
    }

    public async Task<Puzzle> GetDailyPuzzle(CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold("api/puzzle/daily",
            Tuple.Create("cacheBust", Guid.NewGuid().ToString("N")));
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        return await ReadPuzzleResponse(response, cancellationToken);
    }

    public Task<Puzzle> GetRandomPuzzle()
    {
        return GetRandomPuzzle(CancellationToken.None);
    }

    public async Task<Puzzle> GetRandomPuzzle(CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold("api/puzzle/next",
            Tuple.Create("cacheBust", Guid.NewGuid().ToString("N")));
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        return await ReadPuzzleResponse(response, cancellationToken);
    }

    public Task<Puzzle> GetPuzzleByID(string id)
    {
        return GetPuzzleByID(id, CancellationToken.None);
    }

    public async Task<Puzzle> GetPuzzleByID(string id, CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold($"api/puzzle/{id}");
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        return await ReadPuzzleResponse(response, cancellationToken);
    }

    public Task<PuzzleDashboard> GetPuzzleDashboardAsync(int days)
    {
        return GetPuzzleDashboardAsync(days, CancellationToken.None);
    }

    public async Task<PuzzleDashboard> GetPuzzleDashboardAsync(int days,
        CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold($"api/puzzle/dashboard/{days}");
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return LichessJson.Deserialize<PuzzleDashboard>(content);
    }

    public Task<StormDashboard> GetStormDashboardAsync(string username, int days = 30)
    {
        return GetStormDashboardAsync(username, days, CancellationToken.None);
    }

    public async Task<StormDashboard> GetStormDashboardAsync(string username, int days,
        CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold($"api/storm/dashboard/{username}",
            Tuple.Create("days", days.ToString()));
        var response = await SendRequest(request, cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return LichessJson.Deserialize<StormDashboard>(content);
    }

    public Task<PuzzleRace> CreatePuzzleRaceAsync()
    {
        return CreatePuzzleRaceAsync(CancellationToken.None);
    }

    public async Task<PuzzleRace> CreatePuzzleRaceAsync(CancellationToken cancellationToken)
    {
        var request = GetRequestScaffold("api/racer");
        var response = await SendRequest(request, "POST",
            cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return LichessJson.Deserialize<PuzzleRace>(content);
    }

    public async Task<(Puzzle Puzzle, int Rating)> GetRatedPuzzleAsync(
        CancellationToken cancellationToken = default)
    {
        var request = GetRequestScaffold("api/puzzle/batch/mix",
            Tuple.Create("nb", "1"));
        var response = await SendRequest(request,
            cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var root = LichessJson.Parse(content);
        var item = root.GetProperty("puzzles")[0];
        var puzzle = item.GetProperty("puzzle")
            .Deserialize<Puzzle>(LichessJson.Options)
            ?? throw new JsonException(
                "Rated puzzle response did not contain puzzle data.");
        puzzle.Game = item.GetProperty("game")
            .Deserialize<PuzzleGame>(LichessJson.Options)
            ?? throw new JsonException(
                "Rated puzzle response did not contain game data.");
        var rating = (int)Math.Round(
            root.GetProperty("glicko").GetProperty("rating").GetDouble());
        return (puzzle, rating);
    }

    public async Task<(int Rating, int RatingDiff)> SubmitRatedPuzzleAsync(
        string puzzleId,
        bool win,
        CancellationToken cancellationToken = default)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            solutions = new[]
            {
                new { id = puzzleId, win, rated = true }
            }
        });
        var request = GetRequestScaffold("api/puzzle/batch/mix");
        var response = await SendRequest(
            request,
            "POST",
            cancellationToken: cancellationToken,
            requestContent: new StringContent(
                payload, Encoding.UTF8, "application/json"));
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var root = LichessJson.Parse(content);
        var rating = (int)Math.Round(
            root.GetProperty("glicko").GetProperty("rating").GetDouble());
        var ratingDiff = root.GetProperty("rounds")[0]
            .GetProperty("ratingDiff").GetInt32();
        return (rating, ratingDiff);
    }

    private static async Task<Puzzle> ReadPuzzleResponse(LichessResponse response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var json = LichessJson.Parse(content);
        var puzzle = json.GetProperty("puzzle").Deserialize<Puzzle>(LichessJson.Options)
            ?? throw new JsonException("Puzzle response did not contain puzzle data.");
        puzzle.Game = json.GetProperty("game").Deserialize<PuzzleGame>(LichessJson.Options)
            ?? throw new JsonException("Puzzle response did not contain game data.");
        return puzzle;
    }
}



