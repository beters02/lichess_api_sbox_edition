#nullable enable annotations

using LichessNET.Entities.OAuth;
using LichessNET.Internal;

namespace LichessNET.API;

public partial class LichessApiClient
{
    public async Task<OAuthTokenResponse> ExchangeAuthorizationCodeAsync(
        string code,
        string codeVerifier,
        string redirectUri,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(codeVerifier)
            || string.IsNullOrWhiteSpace(redirectUri)
            || string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("OAuth token exchange fields are required.");
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId
        });
        var response = await Sandbox.Http.RequestAsync(
            Constants.BaseUrl + "api/token",
            "POST",
            content,
            new Dictionary<string, string>(),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new LichessApiException(response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<OAuthTokenResponse>(
                body,
                LichessJson.Options)
                ?? throw LichessApiException.InvalidResponse(response.StatusCode);
        }
        catch (Exception exception) when (exception is not OperationCanceledException
            && exception is not LichessApiException)
        {
            throw LichessApiException.InvalidResponse(response.StatusCode);
        }
    }

    public async Task RevokeCurrentTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
            return;
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer " + token
        };
        var response = await Sandbox.Http.RequestAsync(
            Constants.BaseUrl + "api/token",
            "DELETE",
            null,
            headers,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new LichessApiException(response.StatusCode);
    }

    /// <summary>
    /// Tests OAuth tokens without placing them in the request URI or logs.
    /// </summary>
    public Task<Dictionary<string, TokenInfo?>> TestTokensAsync(List<string> tokens)
    {
        return TestTokensAsync(tokens, CancellationToken.None);
    }

    public async Task<Dictionary<string, TokenInfo?>> TestTokensAsync(List<string> tokens,
        CancellationToken cancellationToken = default)
    {
        if (tokens == null)
            throw new ArgumentNullException(nameof(tokens));

        if (tokens.Count == 0 || tokens.Count > 1000)
            throw new ArgumentOutOfRangeException(nameof(tokens),
                "Between 1 and 1000 OAuth tokens are required.");

        if (tokens.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("OAuth tokens cannot be empty.", nameof(tokens));

        cancellationToken.ThrowIfCancellationRequested();

        var request = GetRequestScaffold("api/token/test");
        var requestContent = new StringContent(
            string.Join(',', tokens), Encoding.UTF8, "text/plain");
        var response = await SendRequest(request, "POST", useToken: false,
            cancellationToken: cancellationToken, requestContent: requestContent);

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await response.Content.ReadFromJsonAsync<Dictionary<string, TokenInfo?>>(
                       LichessJson.Options)
                   ?? new Dictionary<string, TokenInfo?>();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw LichessApiException.InvalidResponse(response.StatusCode);
        }
    }

    /// <summary>
    /// Deletes a token.
    /// </summary>
    public async Task DeleteTokenAsync(string token)
    {
        await SetToken(token);
        await RevokeCurrentTokenAsync();
    }
}

