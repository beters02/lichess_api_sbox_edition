using LichessNET.Entities.Teams;
using LichessNET.Entities.Tournament;
using LichessNET.Entities.Tournament.Arena;
using Newtonsoft.Json;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    ///     Get the team with the specified ID
    /// </summary>
    /// <param name="teamId">The ID of the team</param>
    /// <returns>The team with the specified ID</returns>
    public async Task<LichessTeam> GetTeamAsync(string teamId)
    {
        _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/team/{teamId}");
        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var team = JsonConvert.DeserializeObject<LichessTeam>(content);

        return team;
    }

    /// <summary>
    /// Gets the most populat teams on Lichess in a paginated manner.
    /// </summary>
    /// <param name="page">Page number, defaults to 1</param>
    /// <returns>The list of the most popular teams on the specified page.</returns>
    public async Task<List<LichessTeam>> GetPopularTeamsAsync(int page = 1)
    {
        _ratelimitController.Consume();
        var request = GetRequestScaffold("api/team/all", new Tuple<string, string>("page", page.ToString()));
        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var teamspage = JsonConvert.DeserializeObject<dynamic>(content);

        return teamspage["currentPageResults"].ToObject<List<LichessTeam>>();
    }

    /// <summary>
    /// Get the list of teams that the specified user is in.
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <returns>A list of teams that the specified user is in</returns>
    public async Task<List<LichessTeam>> GetTeamOfUserAsync(string username)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/team/of/{username}");
        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var teams = JsonConvert.DeserializeObject<List<LichessTeam>>(content);

        return teams;
    }

    /// <summary>
    /// This funciton gets the members of a team, in chronoclogical order of joining the team. (Latest first),
    /// with up to 5000 members.
    /// </summary>
    /// <param name="teamId">ID of the Team</param>
    /// <returns></returns>
    public async Task<List<TeamMember>> GetTeamMembersAsync(string teamId)
    {
        _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/team/{teamId}/users");

        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var members = JsonConvert.DeserializeObject<List<TeamMember>>(content);

        return members;
    }

    /// <summary>
    /// Retrieves all Swiss tournaments of a team.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="max">The maximum number of tournaments to download. Default is 100.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of Swiss tournaments.</returns>
    public async Task<List<SwissTournament>> GetTeamSwissTournamentsAsync(string teamId, int max = 100)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/team/{teamId}/swiss?max={max}";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var tournaments = new List<SwissTournament>();
        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var tournament = JsonConvert.DeserializeObject<SwissTournament>(line);
                tournaments.Add(tournament);
            }
        }

        return tournaments;
    }

    /// <summary>
    /// Retrieves all Arena tournaments relevant to a team.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="max">The maximum number of tournaments to download. Default is 100.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of Arena tournaments.</returns>
    public async Task<List<ArenaTournament>> GetTeamArenaTournamentsAsync(string teamId, int max = 100)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/team/{teamId}/arena?max={max}";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var tournaments = new List<ArenaTournament>();
        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var tournament = JsonConvert.DeserializeObject<ArenaTournament>(line);
                tournaments.Add(tournament);
            }
        }

        return tournaments;
    }

    /// <summary>
    /// Joins a team.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <param name="message">The message for the team join request, required if the team manually reviews admission requests.</param>
    /// <param name="password">The password for the team, if required.</param>
    /// <returns>A task representing the asynchronous operation, containing the join team response.</returns>
    public async Task<bool> JoinTeamAsync(string teamId, string message = null, string password = null)
    {
        _ratelimitController.Consume();

        var endpoint = $"team/{teamId}/join";
        var request = GetRequestScaffold(endpoint);

        var formData = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(message))
        {
            formData.Add("message", message);
        }

        if (!string.IsNullOrEmpty(password))
        {
            formData.Add("password", password);
        }

        request.Content = new FormUrlEncodedContent(formData);

        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();

        return content.Contains("true");
    }

    /// <summary>
    /// Leaves a team.
    /// </summary>
    /// <param name="teamId">The ID of the team.</param>
    /// <returns>A task representing the asynchronous operation, containing the leave team response.</returns>
    public async Task<bool> LeaveTeamAsync(string teamId)
    {
        _ratelimitController.Consume();

        var endpoint = $"team/{teamId}/quit";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();

        return content.Contains("true");
    }
}