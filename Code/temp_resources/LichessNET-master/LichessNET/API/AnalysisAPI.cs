using LichessNET.Entities.Analysis;
using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;
using Newtonsoft.Json;

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
    public async Task<PositionEvaluation> GetCloudEvaluationAsync(string fen, int multiPv = 1,
        ChessVariant variant = ChessVariant.Standard)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/cloud-eval";
        var request = GetRequestScaffold(endpoint,
            Tuple.Create("fen", fen),
            Tuple.Create("multiPv", multiPv.ToString()),
            Tuple.Create("variant", variant.GetEnumMemberValue()));

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var evaluationResponse = JsonConvert.DeserializeObject<PositionEvaluation>(content);
        return evaluationResponse;
    }
}