#nullable enable annotations

using System.Text.Json.Serialization;

namespace LichessNET.Entities.OAuth;

public sealed class OAuthTokenResponse
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
