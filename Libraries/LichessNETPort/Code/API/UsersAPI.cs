using LichessNET.Entities.Account.Performance;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Social;
using LichessNET.Entities.Social.Stream;
using LichessNET.Extensions;

namespace LichessNET.API;

public partial class LichessApiClient
{
    public async Task<UserRealTimeStatus> GetRealTimeUserStatusAsync(string id, bool withSignal = false,
        bool withGameIds = false, bool withGameMetas = false)
    {
        var users = await GetRealTimeUserStatusAsync(new List<string> { id }, withSignal, withGameIds, withGameMetas);
        return users.Count > 0 ? users[0] : null;
    }

    public async Task<List<UserRealTimeStatus>> GetRealTimeUserStatusAsync(IEnumerable<string> ids,
        bool withSignal = false, bool withGameIds = false, bool withGameMetas = false)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold("api/users/status",
            Tuple.Create("ids", string.Join(",", ids)),
            Tuple.Create("withSignal", withSignal.ToString().ToLowerInvariant()),
            Tuple.Create("withGameIds", withGameIds.ToString().ToLowerInvariant()),
            Tuple.Create("withGameMetas", withGameMetas.ToString().ToLowerInvariant())
        );

        var response = await SendRequest(request);
        return LichessJson.Deserialize<List<UserRealTimeStatus>>(await response.Content.ReadAsStringAsync()) ?? new List<UserRealTimeStatus>();
    }

    public async Task<List<LichessUser>> GetLeaderboardAsync(int nb, Gamemode perfType)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/player/top/{nb}/{perfType.GetApiName()}");
        var response = await SendRequest(request);
        var json = LichessJson.Parse(await response.Content.ReadAsStringAsync());

        return json.TryGetProperty("users", out var users)
            ? users.Deserialize<List<LichessUser>>(LichessJson.Options) ?? new List<LichessUser>()
            : new List<LichessUser>();
    }

    public async Task<Dictionary<Gamemode, List<LichessUser>>> GetAllLeaderboardsAsync()
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold("api/player");
        var response = await SendRequest(request);
        var json = LichessJson.Parse(await response.Content.ReadAsStringAsync());
        var leaderboards = new Dictionary<Gamemode, List<LichessUser>>();

        foreach (var property in json.EnumerateObject())
        {
            if (!Enum.TryParse(property.Name, true, out Gamemode gamemode))
                continue;

            leaderboards[gamemode] = property.Value.Deserialize<List<LichessUser>>(LichessJson.Options) ?? new List<LichessUser>();
        }

        return leaderboards;
    }

    public async Task<CrossTable> GetCrossTableAsync(string user1, string user2, bool includeMatchup = false)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/crosstable/{user1}/{user2}",
            includeMatchup ? Tuple.Create("matchup", "true") : null);

        var response = await SendRequest(request);
        var json = LichessJson.Parse(await response.Content.ReadAsStringAsync());
        var crossTable = new CrossTable
        {
            TotalGames = json.TryGetProperty("nbGames", out var nbGames) ? nbGames.GetInt32() : 0,
            Scores = json.TryGetProperty("users", out var users)
                ? users.Deserialize<Dictionary<string, double>>(LichessJson.Options) ?? new Dictionary<string, double>()
                : new Dictionary<string, double>()
        };

        if (includeMatchup && json.TryGetProperty("matchup", out var matchup))
        {
            crossTable.CurrentMatchup = new Matchup
            {
                TotalGames = matchup.TryGetProperty("nbGames", out var matchupGames) ? matchupGames.GetInt32() : 0,
                Scores = matchup.TryGetProperty("users", out var matchupUsers)
                    ? matchupUsers.Deserialize<Dictionary<string, int>>(LichessJson.Options) ?? new Dictionary<string, int>()
                    : new Dictionary<string, int>()
            };
        }

        return crossTable;
    }

    public async Task<LichessUser> GetUserProfile(string username)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/user/{username}");
        var response = await SendRequest(request);
        return LichessJson.Deserialize<LichessUser>(await response.Content.ReadAsStringAsync());
    }

    public async Task<Dictionary<Gamemode, List<RatingDataPoint>>> GetRatingHistory(string username)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/user/{username}/rating-history");
        var response = await SendRequest(request);
        var json = LichessJson.Parse(await response.Content.ReadAsStringAsync());
        var ratingHistory = new Dictionary<Gamemode, List<RatingDataPoint>>();

        foreach (var item in json.EnumerateArray())
        {
            if (!item.TryGetProperty("name", out var nameElement) ||
                !Enum.TryParse(nameElement.GetString(), true, out Gamemode gamemode) ||
                !item.TryGetProperty("points", out var pointsElement))
            {
                continue;
            }

            var points = pointsElement.Deserialize<List<List<int>>>(LichessJson.Options) ?? new List<List<int>>();
            ratingHistory[gamemode] = points
                .Where(p => p.Count >= 4)
                .Select(p => new RatingDataPoint(p[0], p[1], p[2], p[3]))
                .ToList();
        }

        return ratingHistory;
    }

    public async Task<PerformanceStats> GetUserPerformanceStatsAsync(string username, Gamemode gamemode)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/user/{username}/perf/{gamemode.GetApiName()}");
        var response = await SendRequest(request);
        return LichessJson.Deserialize<PerformanceStats>(await response.Content.ReadAsStringAsync());
    }

    public async Task<List<LiveStreamer>> GetAllLiveStreamers()
    {
        await _ratelimitController.Consume("api/streamer/live", false);
        var request = GetRequestScaffold("api/streamer/live");
        var response = await SendRequest(request);
        return LichessJson.Deserialize<List<LiveStreamer>>(await response.Content.ReadAsStringAsync()) ?? new List<LiveStreamer>();
    }

    public async Task<bool> AddUserNoteAsync(string username, string text)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/user/{username}/note");
        var response = await SendRequest(request, "POST", formData: new Dictionary<string, string> { { "text", text } });
        return (await response.Content.ReadAsStringAsync()).Contains("true");
    }

    public async Task<List<Note>> GetUserNotesAsync(string username)
    {
        await _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/user/{username}/note");
        var response = await SendRequest(request);
        return LichessJson.Deserialize<List<Note>>(await response.Content.ReadAsStringAsync()) ?? new List<Note>();
    }
}



