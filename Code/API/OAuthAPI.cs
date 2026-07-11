#nullable enable annotations

using LichessNET.Entities.OAuth;

namespace LichessNET.API;

public partial class LichessApiClient
{
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
        var request = GetRequestScaffold("api/token");
        request.Headers["Authorization"] = "Bearer " + token;
        await SendRequest(request, "DELETE", useToken: false);
    }
}

