#nullable enable annotations

using LichessNET.Entities.OAuth;
using LichessNET.Internal;

namespace LichessNET.API;

/// <summary>
///     This class represents a client for the lichess API.
///     It handles all ratelimits and requests.
/// </summary>
public partial class LichessApiClient
{
    private const int RateLimitCooldownSeconds = 60;

    private readonly LichessLog _logger;
    private readonly ApiRatelimitController _ratelimitController = new();
    private string? _token;

    public LichessApiClient(bool doLogging = true)
    {
        _logger = new LichessLog("LichessAPIClient", doLogging);
        _ratelimitController.RegisterBucket("api/account", 5, 3, TimeSpan.FromSeconds(15));
        _ratelimitController.RegisterBucket("api/streamer/live", 2, 1, TimeSpan.FromSeconds(5));
        _ratelimitController.RegisterBucket("api/stream/event", 2, 1, TimeSpan.FromSeconds(5));
        _ratelimitController.RegisterBucket("api/board/seek", 1, 1, TimeSpan.FromSeconds(5));
        _ratelimitController.RegisterBucket("api/board/game/stream", 2, 1, TimeSpan.FromSeconds(5));
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

    private Dictionary<string, string> GetRequestHeaders(LichessRequest request, bool useToken = true)
    {
        var headers = new Dictionary<string, string>(request.Headers);
        if (useToken && !string.IsNullOrWhiteSpace(_token))
            headers["Authorization"] = "Bearer " + _token;

        return headers;
    }

    private async Task<LichessResponse> SendRequest(LichessRequest request, string method = null,
        bool useToken = true, Dictionary<string, string> formData = null, bool formDataAsContent = false,
        CancellationToken cancellationToken = default)
    {
        if ( method is null )
            method = "GET";

        HttpContent content = null;

        if (formData != null)
        {
            if (formDataAsContent)
                content = new FormUrlEncodedContent(formData);
            else
                request.AddFormData(formData);
        }

        try
        {
            await _ratelimitController.Consume(new Uri(request.Uri).AbsolutePath, true);

            var headers = GetRequestHeaders(request, useToken);
            var uri = request.BuildUri();
            var safeUri = SanitizeUriForLogging(uri);
            _logger.Information("Sending request to " + safeUri);

            var responseMessage = await Sandbox.Http.RequestAsync(uri, method, content, headers, cancellationToken);
            var responseContent = responseMessage.Content == null
                ? string.Empty
                : await responseMessage.Content.ReadAsStringAsync();
            var responseHeaders = responseMessage.Headers.ToDictionary(x => x.Key, x => string.Join(",", x.Value));

            if (responseMessage.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _ratelimitController.ReportBlock(RateLimitCooldownSeconds);
            }

            if (!responseMessage.IsSuccessStatusCode)
                throw new HttpRequestException($"Lichess API returned {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}: {responseContent}");

            _logger.Information("Request to " + safeUri + " successful.");
            _logger.Debug("Response: \n" + SanitizeResponseForLogging(uri, responseContent));
            return new LichessResponse(responseContent, responseMessage.StatusCode, responseHeaders);
        }
        finally
        {
            content?.Dispose();
        }
    }

    private static string SanitizeUriForLogging(string uri)
    {
        try
        {
            var builder = new UriBuilder(uri);
            var query = builder.Query.TrimStart('?');
            if (string.IsNullOrWhiteSpace(query))
                return uri;

            var sanitized = query.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part =>
                {
                    var pieces = part.Split('=', 2);
                    var key = Uri.UnescapeDataString(pieces[0]);
                    return key.Contains("token", StringComparison.OrdinalIgnoreCase)
                        ? Uri.EscapeDataString(key) + "=[redacted]"
                        : part;
                });

            builder.Query = string.Join("&", sanitized);
            return builder.Uri.ToString();
        }
        catch
        {
            return uri;
        }
    }

    private static string SanitizeResponseForLogging(string uri, string content)
    {
        return uri.Contains("/api/token", StringComparison.OrdinalIgnoreCase)
            ? "[redacted token response]"
            : content;
    }
}


