#nullable enable annotations

using LichessNET.Entities.OAuth;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Tests if tokens are valid. Uses a query fallback because raw request bodies are not whitelist-safe in s&box.
    /// </summary>
    public async Task<Dictionary<string, TokenInfo?>> TestTokensAsync(List<string> tokens)
    {
        var request = GetRequestScaffold("api/token/test");
        request.Query["tokens"] = string.Join(',', tokens);
        var response = await SendRequest(request, "POST", useToken: false);
        return await response.Content.ReadFromJsonAsync<Dictionary<string, TokenInfo?>>(LichessJson.Options)
               ?? new Dictionary<string, TokenInfo?>();
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

