#nullable enable annotations

using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;

namespace LichessNET.Database;

/// <summary>
/// Database downloads are intentionally unavailable in the whitelist-safe s&box port.
/// </summary>
public class DatabaseClient
{
    private readonly LichessLog _logger = new("DatabaseClient");

    public Task DownloadMonthlyDatabase(int year, int month, string filename, bool forceDownload = false)
    {
        LogKnownIssues(year, month);
        ValidateDate(year, month);
        return UnsupportedDownload();
    }

    public Task DownloadMonthlyDatabase(int year, int month, ChessVariant variant, string filename,
        bool forceDownload = false)
    {
        if (variant == ChessVariant.Racer || variant == ChessVariant.Storm || variant == ChessVariant.Streak)
            throw new ArgumentOutOfRangeException(nameof(variant), "This variant is not supported by the database.");

        ValidateDate(year, month);
        _ = variant.GetApiName();
        return UnsupportedDownload();
    }

    private Task UnsupportedDownload()
    {
        _logger.Warning("Lichess database file downloads are disabled in the whitelist-safe s&box port.");
        throw new NotSupportedException("Lichess database file downloads require file system access that is not whitelist-safe in s&box.");
    }

    private static void ValidateDate(int year, int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12");

        if (year < 2013)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be 2013 or later");
    }

    private void LogKnownIssues(int year, int month)
    {
        var knownIssues = new Dictionary<(int year, int month), string>
        {
            { (2023, 11), "Some Chess960 rematches were recorded with invalid castling rights in their starting FEN." },
            { (2022, 12), "Some Antichess games were recorded with bullet ratings." },
            { (2021, 3), "Some games have incorrect results due to a database outage in the aftermath of a datacenter fire." },
            { (2021, 2), "Some games were resigned even after the game ended. In variants, additional moves could be played after the end of the game." },
            { (2020, 12), "Many variant games have been mistakenly analyzed using standard NNUE, leading to incorrect evaluations." },
            { (2021, 1), "Many variant games have been mistakenly analyzed using standard NNUE, leading to incorrect evaluations." },
            { (2020, 7), "Many games, especially variant games, may have incorrect evaluations in the opening (up to 15 plies)." },
            { (2020, 8), "Many games, especially variant games, may have incorrect evaluations in the opening (up to 15 plies)." },
            { (2016, 12), "Many games may have incorrect evaluations." },
            { (2016, 6), "Some players were able to play themselves in rated games." },
            { (2016, 8), "7 games with illegal castling moves were recorded." }
        };

        if (knownIssues.TryGetValue((year, month), out var issue))
            _logger.Warning($"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: {issue}");
        else if (year < 2016)
            _logger.Warning($"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: In some cases, mate may not be forced in the number of moves given by the evaluations.");
        else if (year == 2020 && month <= 12)
            _logger.Warning($"Known issue for {year}-{month.ToString().PadLeft(2, '0')}: Some exports are missing redundant PGN tags and black move numbers after comments.");
    }
}
