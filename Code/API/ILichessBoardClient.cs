#nullable enable annotations

using LichessNET.Entities.Board;
using LichessNET.Entities.OAuth;

namespace LichessNET.API;

/// <summary>
/// Operations required to seek and play games through the Lichess Board API.
/// </summary>
public interface ILichessBoardClient
{
    string? GetToken();

    Task<Dictionary<string, TokenInfo?>> TestTokensAsync(List<string> tokens,
        CancellationToken cancellationToken = default);

    Task CreateBoardSeekAsync(BoardSeekOptions options, CancellationToken cancellationToken = default);

    Task<ILichessBoardEventStream> CreateBoardAccountEventStreamAsync(
        CancellationToken cancellationToken = default);

    Task<ILichessBoardEventStream> CreateBoardGameStreamAsync(string gameId,
        CancellationToken cancellationToken = default);

    Task<bool> MakeBoardMoveAsync(string gameId, string uci, bool offerDraw = false,
        CancellationToken cancellationToken = default);

    Task<bool> AbortBoardGameAsync(string gameId, CancellationToken cancellationToken = default);

    Task<bool> ResignBoardGameAsync(string gameId, CancellationToken cancellationToken = default);

    Task<bool> HandleDrawOfferAsync(string gameId, bool accept,
        CancellationToken cancellationToken = default);

    Task<bool> SendBoardChatAsync(string gameId, string text,
        BoardChatRoom room = BoardChatRoom.Player, CancellationToken cancellationToken = default);
}
