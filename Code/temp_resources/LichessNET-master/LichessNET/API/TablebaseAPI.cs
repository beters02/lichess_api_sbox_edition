using LichessNET.Entities.Analysis;
using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;
using Newtonsoft.Json;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Checks if a position is avaliable in the tablebases on lichess.
    /// </summary>
    /// <param name="fen">The position to check</param>
    /// <param name="variant">The variant for which a position should be looked up. Currently supported are
    /// Standard, Chess960, Atomic and Antichess
    /// </param>
    /// <returns>A TablebaseLookup object</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid variant was requested</exception>
    public async Task<TablebaseLookup> GetTablebaseLookupAsync(string fen, ChessVariant variant = ChessVariant.Standard)
    {
        if (variant != ChessVariant.Standard || variant != ChessVariant.Chess960 || variant != ChessVariant.Atomic ||
            variant != ChessVariant.Antichess)
        {
            throw new ArgumentException(
                "Tablebase lookups are only supported for standard, Chess960, Atomic and Antichess");
        }

        //Beacuse the API does not support Chess960, we need to convert it to standard, as the moves are just the same as in normal chess.
        if (variant == ChessVariant.Chess960) variant = ChessVariant.Standard;


        var _httpClient = new HttpClient();
        var url = $"https://tablebase.lichess.ovh/{variant.GetEnumMemberValue()}?fen={Uri.EscapeDataString(fen)}";
        var response = await _httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TablebaseLookup>(jsonResponse);
    }
}