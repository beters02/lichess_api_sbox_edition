using System.Net.Http.Json;
using LichessNET.Entities;
using LichessNET.Entities.Account;
using LichessNET.Entities.Social;
using LichessNET.Entities.Social.Timeline;
using Newtonsoft.Json;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    ///     Retrieves the email address of the authenticated user.
    /// </summary>
    /// <returns>
    ///     A string representing the email address of the authenticated user.
    /// </returns>
    public async Task<string> GetAccountEmail()
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold("api/account/email");

        var response = await SendRequest(request);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return content["email"];
    }

    /// <summary>
    /// Retrieves the profile information of the authenticated user from the Lichess platform.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="LichessUser"/> representing the profile details of the user.
    /// </returns>
    public async Task<LichessUser> GetOwnProfile()
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold("api/account");
        var response = await SendRequest(request);
        return await response.Content.ReadFromJsonAsync<LichessUser>();
    }

    /// <summary>
    /// Retrieves the account preferences of the authenticated user.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="AccountPreferences"/> representing the user's account preferences.
    /// </returns>
    public async Task<AccountPreferences> GetAccountPreferences()
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold("api/account/preferences");

        var response = await SendRequest(request);


        var preferences = await response.Content.ReadFromJsonAsync<AccountPreferences>();
        return preferences;
    }

    /// <summary>
    ///     Determines whether the authenticated user's account is in kid mode.
    /// </summary>
    /// <returns>
    ///     A boolean value indicating if the user's account is set to kid mode.
    /// </returns>
    public async Task<bool> GetKidModeStatus()
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold("api/account/kid");

        var response = await SendRequest(request);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        return content["kid"];
    }

    /// <summary>
    /// Sets the kid mode status for the authenticated account.
    /// </summary>
    /// <param name="enable">
    /// A boolean value indicating whether to enable or disable kid mode.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the operation was successful.
    /// </returns>
    public async Task<bool> SetKidModeStatus(bool enable)
    {
        var request = GetRequestScaffold("api/account/kid", Tuple.Create("v", enable.ToString()));
        var response = await SendRequest(request, HttpMethod.Post);

        return JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync()).ok.ToObject<bool>();
    }

    /// <summary>
    /// Follows a player with the specified username on Lichess.
    /// </summary>
    /// <param name="username">
    /// The username of the player to follow.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// </returns>
    public async Task<bool> FollowPlayerAsync(string username)
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold($"api/rel/follow/{username}");
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        return content["ok"];
    }

    /// <summary>
    /// Unfollows a specified player using their username.
    /// </summary>
    /// <param name="username">
    /// The username of the player to unfollow.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// </returns>
    public async Task<bool> UnfollowPlayerAsync(string username)
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold($"api/rel/unfollow/{username}");
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        return content["ok"];
    }

    /// <summary>
    /// Blocks a player on Lichess based on their username.
    /// </summary>
    /// <param name="username">
    /// The username of the player to be blocked.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the player was successfully blocked.
    /// </returns>
    public async Task<bool> BlockPlayerAsync(string username)
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold($"api/rel/block/{username}");
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        return content["ok"];
    }

    /// <summary>
    /// Unblocks a player specified by username.
    /// </summary>
    /// <param name="username">
    /// The username of the player to unblock.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the player was successfully unblocked.
    /// </returns>
    public async Task<bool> UnblockPlayerAsync(string username)
    {
        _ratelimitController.Consume("api/account", false);

        var request = GetRequestScaffold($"api/rel/unblock/{username}");
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, bool>>();
        return content["ok"];
    }

    /// <summary>
    /// Gets the timeline of the authenticated user.
    /// </summary>
    /// <param name="since">When the timeline should start</param>
    /// <param name="nb">The number of timeline events that should be loaded. Defaults to 15, the maximum is 50.</param>
    /// <returns>A Timeline object</returns>
    public async Task<Timeline?> GetTimelineAsync(DateTime since, int nb = 15)
    {
        _ratelimitController.Consume("api/timeline", false);

        var unixTimestamp = new DateTimeOffset(since).ToUnixTimeMilliseconds();
        var request = GetRequestScaffold($"api/timeline",
            new Tuple<string, string>("since", unixTimestamp.ToString()),
            new Tuple<string, string>("nb", nb.ToString()));

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<Timeline>(content, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = new List<JsonConverter> { new TimelineEventDataConverter() }
        });
    }
}