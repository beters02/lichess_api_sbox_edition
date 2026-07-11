#nullable enable annotations

namespace LichessNET.API;

/// <summary>
/// A sanitized failure returned by the Lichess API.
/// </summary>
public sealed class LichessApiException : HttpRequestException
{
    public LichessApiException(HttpStatusCode? statusCode = null)
        : this(CreateMessage(statusCode), statusCode)
    {
    }

    private LichessApiException(string message, HttpStatusCode? statusCode)
        : base(message, null, statusCode)
    {
    }

    internal static LichessApiException InvalidResponse(HttpStatusCode statusCode)
    {
        return new LichessApiException(
            "Lichess API returned a response that could not be processed.", statusCode);
    }

    private static string CreateMessage(HttpStatusCode? statusCode)
    {
        return statusCode.HasValue
            ? $"Lichess API request failed with HTTP status {(int)statusCode.Value} ({statusCode.Value})."
            : "Lichess API request failed before an HTTP status was received.";
    }
}
