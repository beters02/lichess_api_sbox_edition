# LichessNET Port

LichessNET Port is an s&box library for calling the Lichess HTTP API through
`Sandbox.Http`. It includes clients for accounts, analysis, games, OAuth,
puzzles, teams, users, and a custom Board API layer for playing games from an
s&box project.

## Setup

Add `LichessNET Port` as a library dependency, then import the namespaces used
by your feature:

```csharp
using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Entities.Enumerations;
using LichessNET.Gameplay;
```

Create one client and reuse it. Authenticated calls require a Lichess OAuth
token; Board API calls require the `board:play` scope.

```csharp
var lichess = new LichessApiClient();
await lichess.SetToken(oauthToken);
```

Treat the token as a process-session secret. Keep it only on a local,
non-networked owner; never write it to scenes, settings, files, logs, exception
messages, RPC arguments, or replicated properties. Call `SetToken(null)` on
explicit disconnect or application shutdown. Switching features does not require
persisting the token. `LichessQueue.StartSeekAsync` verifies that the configured
token has the required play permission before opening a seek.

## Custom Board API

The board integration has two levels:

- `LichessApiClient` exposes the Lichess Board endpoints directly.
- `LichessQueue` and `LichessGameSession` manage matchmaking, NDJSON streams,
  move synchronization, clocks, chat, and rollback through an
  `IChessBoardAdapter`.

### Stream and owner lifecycle

Give each queue or game session one clear owner and keep it alive for the full
operation. Cancel that owner's token and dispose the queue/session when changing
mode, leaving its scene, or shutting down. Disposal is the terminal operation and
releases linked requests and streams.

`CreateBoardAccountEventStreamAsync` and `CreateBoardGameStreamAsync` return
unstarted streams so handlers can be attached before `Start()`. The legacy
`StreamBoardAccountEventsAsync` and `StreamBoardGameAsync` methods remain
available and return already-started streams. Queue and session helpers use the
unstarted factories internally to avoid losing the first event.

### Seek and start a game

`LichessQueue` opens the account event stream before posting the seek, so it can
raise `OnGameFound` as soon as Lichess assigns a game.

```csharp
private LichessApiClient _lichess;
private LichessQueue _queue;
private LichessGameSession _session;

private async Task StartMatchmakingAsync(
    string oauthToken,
    CancellationToken cancellationToken = default)
{
    _lichess = new LichessApiClient();
    await _lichess.SetToken(oauthToken);

    _queue = new LichessQueue(_lichess);
    _queue.OnGameFound += HandleGameFound;
    _queue.OnError += (_, error) => Log.Error(error.Message);

    await _queue.StartSeekAsync(new BoardSeekOptions
    {
        Rated = false,
        TimeMinutes = 5,
        IncrementSeconds = 3,
        Color = BoardColorPreference.Random,
        Variant = ChessVariant.Standard,
        RatingRange = "1200-1800"
    }, cancellationToken);
}

private void HandleGameFound(
    LichessQueue queue,
    LichessGameFoundEventArgs game)
{
    Log.Info($"Game {game.GameId}; playing as {game.Color}");
    _ = StartGameAsync(queue.Api, game);
}
```

For correspondence games, set `DaysPerTurn` from 1 through 14. When
`DaysPerTurn` is present, `TimeMinutes` and `IncrementSeconds` are not sent.
Realtime seeks accept a clock from greater than 0 through 180 minutes and an
increment from 0 through 180 seconds.

### Connect a board and handle events

A session consumes `gameFull`, `gameState`, and `chatLine` stream messages. It
passes remote moves to the adapter and emits gameplay-friendly events.

```csharp
private async Task StartGameAsync(
    LichessApiClient api,
    LichessGameFoundEventArgs game)
{
    IChessBoardAdapter board = new MyChessBoardAdapter();
    _session = new LichessGameSession(
        api,
        game.GameId,
        game.Color,
        board);

    _session.OnOpponentMove += (_, uci) =>
        Log.Info("Opponent played " + uci);

    _session.OnClockUpdate += (_, clock) =>
        Log.Info($"White: {clock.WhiteTime}; Black: {clock.BlackTime}");

    _session.OnChatLine += (_, chat) =>
        Log.Info($"[{chat.Room}] {chat.Username}: {chat.Text}");

    _session.OnGameOver += (_, state) =>
        Log.Info($"Game ended: {state.Status}; winner: {state.Winner}");

    _session.OnDesync += (_, reason) => Log.Warning(reason);
    _session.OnError += (_, error) => Log.Error(error.Message);

    await _session.StartAsync();
}
```

Keep the queue and session alive while their streams are needed. Dispose both
when the owning component is destroyed:

```csharp
protected override async void OnDestroy()
{
    if (_session is not null)
        await _session.DisposeAsync();

    if (_queue is not null)
        await _queue.DisposeAsync();
}
```

### Reconnection and event threading

`LichessGameSessionOptions` enables automatic reconnect by default. Its delays
are 1, 2, 5, 10, and 15 seconds; the final delay repeats. Connection-state events
let a UI show connected, reconnecting, and finished states. `ReconnectAsync`
also supports an explicit retry while retaining confirmed history and a pending
local move. Authentication failures, terminal game state, and explicit disposal
stop retries.

Stream and session events can arrive on a background continuation. Event handlers
must not directly mutate s&box components, scenes, or Razor state. Enqueue their
data and apply it from the owning component's main-thread update.

### Implement a custom board adapter

The adapter is the boundary between Lichess UCI moves and your board, rules
engine, or presentation. State snapshots let `LichessGameSession` roll back an
optimistically applied local move if Lichess rejects it.

```csharp
public sealed class MyChessBoardAdapter : IChessBoardAdapter
{
    private readonly MyChessBoard _board = new();

    public bool TryApplyLocalMove(string uci)
    {
        // Validate turn ownership and legality before changing local state.
        return _board.TryMove(UciMove.Parse(uci));
    }

    public bool TryApplyRemoteMove(string uci)
    {
        // Apply a move already confirmed by the Lichess game stream.
        return _board.TryMove(UciMove.Parse(uci));
    }

    public string ExportState()
    {
        // FEN is a convenient choice, but any lossless format is valid.
        return _board.ToFen();
    }

    public void ImportState(string state)
    {
        _board.LoadFen(state);
    }
}
```

For a game whose initial `gameFull` event contains a non-starting FEN, also
implement `IChessInitialPositionAdapter.TrySetInitialPosition`. The session calls
it before replaying authoritative UCI history. Adapters that support only the
normal starting position can omit this optional interface; they must not silently
apply history to the wrong initial board.

`RecordingChessBoardAdapter` is available for smoke tests. It validates UCI
syntax and records moves, but it is not a chess rules engine and does not check
move legality.

### Submit moves and game actions

Use the session for normal gameplay. A local move is applied immediately, sent
to Lichess, then confirmed by the stream. Only one local move may wait for
confirmation at a time.

```csharp
// Standard UCI move.
bool moved = await _session.SubmitLocalMoveAsync("e2e4");

// Promotion and a draw offer attached to the move.
bool promoted = await _session.SubmitLocalMoveAsync("e7e8q", offerDraw: true);

await _session.SendChatAsync("Good luck!");
await _session.SendChatAsync("Spectator update", BoardChatRoom.Spectator);

await _session.AcceptDrawAsync();
await _session.DeclineDrawAsync();
await _session.ResignAsync();
// AbortAsync is intended for games that are still abortable.
```

Coordinates can be converted from UCI notation for an s&box board. Files map
left-to-right to `0..7`; ranks map top-to-bottom to `0..7`.

```csharp
UciMove move = UciMove.Parse("e2e4");

int fromX = move.From.X; // 4
int fromY = move.From.Y; // 6
int toX = move.To.X;     // 4
int toY = move.To.Y;     // 4
```

### Use the endpoints directly

The lower-level client is useful when another system owns game state or stream
lifecycle.

```csharp
await using var stream = await _lichess.StreamBoardGameAsync(gameId);

stream.LineReceived += (_, json) =>
{
    switch (BoardEventParser.GetEventType(json))
    {
        case "gameFull":
        {
            BoardGameFullEvent full = BoardEventParser.ParseGameFull(json);
            Log.Info("Initial moves: " + string.Join(", ", full.State.MoveList));
            break;
        }
        case "gameState":
        {
            BoardGameState state = BoardEventParser.ParseGameState(json);
            Log.Info($"Status: {state.Status}; moves: {state.Moves}");
            break;
        }
        case "chatLine":
        {
            BoardChatLineEvent chat = BoardEventParser.ParseChatLine(json);
            Log.Info(chat.Username + ": " + chat.Text);
            break;
        }
    }
};

bool accepted = await _lichess.MakeBoardMoveAsync(gameId, "g1f3");
bool sent = await _lichess.SendBoardChatAsync(
    gameId,
    "Hello!",
    BoardChatRoom.Player);
```

The returned `LichessNdjsonStream` is already started. Subscribe immediately,
handle `ErrorReceived` where appropriate, and dispose the stream to cancel it.

## Analysis, puzzles, ongoing games, and export

Cancellation-token overloads are available for token testing, puzzles, cloud
analysis, ongoing-game lookup, and single-game export. Legacy overloads remain
and use `CancellationToken.None`. Pass the owning feature's token so cancellation
interrupts both rate-limit waits and HTTP work.

```csharp
using var cancellation = new CancellationTokenSource();

Puzzle puzzle = await lichess.GetDailyPuzzle(cancellation.Token);
PositionEvaluation evaluation = await lichess.GetCloudEvaluationAsync(
    fen,
    multiPv: 3,
    variant: ChessVariant.Standard,
    cancellationToken: cancellation.Token);
List<OngoingGame> games = await lichess.GetOngoingGamesAsync(
    9,
    cancellation.Token);

var exportOptions = new GameExportOptions
{
    Clocks = true,
    Evals = true,
    Literate = false
};

string rawPgn = await lichess.GetGamePgnAsync(
    gameId,
    exportOptions,
    cancellation.Token);
Game parsed = Game.FromPgn(rawPgn);
```

Cloud analysis rejects empty FEN and a `multiPv` outside 1 through 5 before
sending a request. Each principal variation exposes legacy `Cp`, nullable
`Mate`, a kind-preserving `Score`, and whitespace-parsed `UciMoves`. There is
no batch cloud endpoint: callers should evaluate positions sequentially with
cancellation and their own FEN cache.

`GetGamePgnAsync` returns the response losslessly. `Game.FromPgn` copies it to
`RawPgn` and parses only mainline SAN; it skips comments, NAGs, and recursive
variations while retaining clock/evaluation comment metadata. SAN-to-UCI
conversion belongs in the consuming chess rules layer.

## Board API reference

| Member | Purpose |
| --- | --- |
| `CreateBoardSeekAsync(options, ct)` | Create a realtime or correspondence seek. |
| `CreateBoardAccountEventStreamAsync(ct)` | Create an unstarted account event stream. |
| `CreateBoardGameStreamAsync(gameId, ct)` | Create an unstarted game event stream. |
| `StreamBoardAccountEventsAsync(ct)` | Return a started account event stream. |
| `StreamBoardGameAsync(gameId, ct)` | Return a started game event stream. |
| `MakeBoardMoveAsync(gameId, uci, offerDraw, ct)` | Submit a UCI move. |
| `AbortBoardGameAsync(gameId, ct)` | Abort an eligible game. |
| `ResignBoardGameAsync(gameId, ct)` | Resign a game. |
| `HandleDrawOfferAsync(gameId, accept, ct)` | Accept or decline a draw offer. |
| `SendBoardChatAsync(gameId, text, room, ct)` | Send player or spectator chat. |

All action methods returning `bool` report whether Lichess accepted the action.
Invalid game IDs, empty chat, invalid UCI, and invalid seek ranges fail locally
with argument exceptions. HTTP failures, including authentication and API
errors, are surfaced as `HttpRequestException`.

## Notes

- UCI moves use forms such as `e2e4` and `e7e8q`.
- Board stream clocks are exposed in milliseconds and as nullable `TimeSpan`
  values through `BoardClockState`.
- Lichess limits concurrent streams per IP. The library allows at most eight
  active `LichessNdjsonStream` instances and warns above five.
- The library rate-limits known endpoint groups and observes Lichess `429`
  responses.

