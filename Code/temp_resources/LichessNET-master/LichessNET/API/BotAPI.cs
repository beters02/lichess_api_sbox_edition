using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Social;
using Newtonsoft.Json;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Accepts a challenge from Lichess
    /// </summary>
    /// <param name="challengeId">The challenge ID to accept</param>
    /// <returns>A bool whether the operation was successful or not</returns>
    public async Task<bool> AcceptChallengeAsync(string challengeId)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/challenge/{challengeId}/accept";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request, HttpMethod.Post);
        return response.Content.ReadAsStringAsync().Result.Contains("true");
    }

    /// <summary>
    /// Declines a challenge from Lichess
    /// </summary>
    /// <param name="challengeId">The challenge ID to decline</param>
    /// <param name="reason">The reason why the challenge is declined. Default is ChallengeDeniedReason.Generic</param>
    /// <returns>A bool whether the operation was successful or not</returns>
    public async Task<bool> DeclineChallengeAsync(string challengeId,
        ChallengeDeniedReason reason = ChallengeDeniedReason.Generic)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/challenge/{challengeId}/decline";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request, HttpMethod.Post);
        return response.Content.ReadAsStringAsync().Result.Contains("true");
    }

    /// <summary>
    /// Gets all challenges by a user. Those are sorted by incoming and outgoing challenges.
    /// </summary>
    /// <returns>A struct with all challenges</returns>
    public async Task<ChallengeResponse> GetChallengesAsync()
    {
        _ratelimitController.Consume();

        var endpoint = "api/challenge";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var challengeResponse = JsonConvert.DeserializeObject<ChallengeResponse>(content);
        return challengeResponse;
    }

    /// <summary>
    /// Challenge a user to a game on Lichess
    /// </summary>
    /// <param name="username">The user to challenge</param>
    /// <param name="rated">Whether the game should be rated or not</param>
    /// <param name="clockLimit">The clock limit</param>
    /// <param name="clockIncrement">The increment on the clock</param>
    /// <param name="days">Days per move (for correspondence)</param>
    /// <param name="color">The color you want to play</param>
    /// <param name="variant">The variant to play</param>
    /// <param name="fen">The starting FEN</param>
    /// <param name="keepAliveStream">Keep the challenge alive after 20 seconds</param>
    /// <param name="rules">Rules</param>
    /// <returns>The challenge entity</returns>
    public async Task<Challenge> ChallengeUserAsync(string username, bool rated = false, int clockLimit = 0,
        int clockIncrement = 0, int? days = null, string color = "random", string variant = "standard",
        string fen = null, bool keepAliveStream = false, string rules = null)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/challenge/{username}";
        var request = GetRequestScaffold(endpoint);

        var parameters = new Dictionary<string, string>
        {
            { "rated", rated.ToString().ToLower() },
            { "clock.limit", clockLimit.ToString() },
            { "clock.increment", clockIncrement.ToString() },
            { "color", color },
            { "variant", variant }
        };

        if (days.HasValue)
        {
            parameters.Add("days", days.Value.ToString());
        }

        if (!string.IsNullOrEmpty(fen))
        {
            parameters.Add("fen", fen);
        }

        if (keepAliveStream)
        {
            parameters.Add("keepAliveStream", "true");
        }

        if (!string.IsNullOrEmpty(rules))
        {
            parameters.Add("rules", rules);
        }

        request.Content = new FormUrlEncodedContent(parameters);
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();

        var challengeResponse = JsonConvert.DeserializeObject<Challenge>(content);
        return challengeResponse;
    }
}