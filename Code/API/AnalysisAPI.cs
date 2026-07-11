#nullable enable annotations

using LichessNET.Entities.Analysis;
using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Gets the evaluation of a position from the Lichess cloud analysis.
    /// </summary>
    /// <param name="fen">The FEN of the position</param>
    /// <param name="multiPv">How much different variants to include in the analysis. Can go up to 5.</param>
    /// <param name="variant">Which chess variant the game is from</param>
    /// <returns>A PositionEvaluation object</returns>
    public Task<PositionEvaluation> GetCloudEvaluationAsync(string fen, int multiPv = 1,
        ChessVariant variant = ChessVariant.Standard)
    {
        return GetCloudEvaluationAsync(fen, multiPv, variant, CancellationToken.None);
    }

    /// <summary>
    /// Gets the evaluation of a position and observes cancellation while waiting
    /// for rate limiting and the HTTP response.
    /// </summary>
    public async Task<PositionEvaluation> GetCloudEvaluationAsync(string fen, int multiPv,
        ChessVariant variant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fen))
            throw new ArgumentException("FEN cannot be empty.", nameof(fen));

        if (multiPv < 1 || multiPv > 5)
            throw new ArgumentOutOfRangeException(nameof(multiPv), multiPv,
                "The number of principal variations must be between 1 and 5.");

        cancellationToken.ThrowIfCancellationRequested();

        var endpoint = "api/cloud-eval";
        var request = GetRequestScaffold(endpoint,
            Tuple.Create("fen", fen),
            Tuple.Create("multiPv", multiPv.ToString()),
            Tuple.Create("variant", variant.GetApiName()));

        var response = await SendRequest(request, cancellationToken: cancellationToken);
        var content = await response.Content.ReadAsStringAsync();
        cancellationToken.ThrowIfCancellationRequested();

        return LichessJson.Deserialize<PositionEvaluation>(content);
    }
}


