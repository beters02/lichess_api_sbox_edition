using System.Net;
using LichessNET.Entities.OAuth;
using LichessNET.Internal;

namespace LichessNET.API;

/// <summary>
///     This class represents a client for the lichess API.
///     It handles all ratelimits and requests.
/// </summary>
public partial class LichessApiClient
{
    private readonly LichessLog _logger;
    private readonly ApiRatelimitController _ratelimitController = new();
    private string? _token;

    public LichessApiClient(bool doLogging = true)
    {
        _logger = new LichessLog("LichessAPIClient", doLogging);
        _ratelimitController.RegisterBucket("api/account", 5, 3, TimeSpan.FromSeconds(15));
        _ratelimitController.RegisterBucket("api/streamer/live", 2, 1, TimeSpan.FromSeconds(5));
    }

    public string? GetToken() => _token;

    public Task SetToken(string? value)
    {
        _token = value;
        return Task.CompletedTask;
    }

    private UriBuilder GetUriBuilder(string endpoint, params Tuple<string, string>[] queryParameters)
    {
        var builder = new UriBuilder(Constants.BaseUrl + endpoint)
        {
            Port = -1
        };

        var query = new List<string>();
        var existingQuery = builder.Query.TrimStart('?');
        if (!string.IsNullOrWhiteSpace(existingQuery))
            query.Add(existingQuery);

        foreach (var param in queryParameters)
        {
            if (param == null || string.IsNullOrWhiteSpace(param.Item1) || param.Item2 == null)
                continue;

            query.Add($"{Uri.EscapeDataString(param.Item1)}={Uri.EscapeDataString(param.Item2)}");
        }

        builder.Query = string.Join("&", query);
        return builder;
    }

    private LichessRequest GetRequestScaffold(string endpoint, params Tuple<string, string>[] queryParameters)
    {
        return new LichessRequest(GetUriBuilder(endpoint, queryParameters).Uri.ToString());
    }

    private async Task<LichessResponse> SendRequest(LichessRequest request, string method = null,
        bool useToken = true, Dictionary<string, string> formData = null)
    {
        method ??= "GET";
        if (formData != null)
            request.AddFormData(formData);

        await _ratelimitController.Consume(new Uri(request.Uri).AbsolutePath, true);

        var headers = new Dictionary<string, string>(request.Headers);
        if (useToken && !string.IsNullOrWhiteSpace(_token))
            headers["Authorization"] = "Bearer " + _token;

        var uri = request.BuildUri();
        _logger.Information("Sending request to " + uri);
        var content = await Sandbox.Http.RequestStringAsync(uri, method, null, headers, CancellationToken.None);
        _logger.Information("Request to " + uri + " successful.");
        _logger.Debug("Response: \n" + content);
        return new LichessResponse(content);
    }
}


