# Rough Integration Shape
- Make a thin “Lichess gameplay layer” on top of the API port:
- LichessQueue
- Handles token, time control, rated/casual, color.
- Calls Lichess Board API seek/challenge endpoints.
- Emits OnGameFound(gameId, color).

# LichessGameSession
- Opens Lichess board game stream.
- Parses NDJSON events: gameFull, gameState, chatLine.
- Tracks server move list as UCI: e2e4 e7e5 g1f3.
- Sends player moves to Lichess: POST /api/board/game/{gameId}/move/{uci}.
- Emits OnOpponentMove(uci), OnGameOver(result), OnClockUpdate.

# IChessBoardAdapter
- Lets any s&box chess game plug in without rewriting board logic.

`
public interface IChessBoardAdapter
{
	bool TryApplyLocalMove( string uci );
	void ApplyRemoteMove( string uci );
	string ExportState();
	void ImportState( string state );
}
`

# For kachess, the adapter wraps ChessBoard.TryMovePiece(...).
- UCI mapping should be simple:
`
// e2 -> x=4, y=6 if y=0 is black back rank
static (int x, int y) FromSquare( string sq )
{
	int x = sq[0] - 'a';
	int rank = sq[1] - '0';
	int y = 8 - rank;
	return (x, y);
}
//Then:
public bool TryApplyLocalMove( string uci )
{
	var from = FromSquare( uci[..2] );
	var to = FromSquare( uci[2..4] );
	return Board.TryMovePiece( from.x, from.y, to.x, to.y );
}
`

# Recommended User Flow
- Player opens your chess game:
- Paste Lichess OAuth token with board:play.
- Pick casual/rated, clock, increment, color.
- Click “Find Lichess Game”.
- When matched, your game locks sides.
- Local drag/drop becomes UCI.
- Send UCI to Lichess.
- Treat Lichess stream as authoritative and sync board from move list.

# Important Note
Your current port has challenge/bot/game read APIs, but for real human queue/play you’ll want to add the Lichess Board API layer next: seek, stream game state, make move, resign, abort, draw, chat. That can be a small extra file, and then kachess only needs an adapter around ChessBoard