# Kachess × Lichess Implementation Plan

## Summary

- Deliver a queue-first vertical slice: mode switch, session-only token,
  standard realtime matchmaking, complete play, chat, and game actions.
- Keep native Kachess multiplayer authoritative and unchanged in behavior.
- Share only a pure chess model and board presentation between providers.
- Implement required Board API hardening before wiring live play.
- Add daily/random puzzles, analysis, history review, then the full puzzle hub
  as separate passes.

## Kachess Integration Plan

### 1. Extract the shared chess foundation

- Add characterization tests around current native moves, RPC flow, clocks, and
  board orientation before refactoring.
- Extract `ChessMatchModel`, independent of `Component`, networking, scenes, and
  Lichess. It owns:
  - pieces, side to move, castling rights, en-passant state;
  - halfmove/fullmove counters and repetition history;
  - last move and termination reason;
  - legal move generation and application.
- Support all standard moves, including castling, en passant, and
  `q/r/b/n` promotions. Add checkmate, stalemate, repetition, fifty-move, and
  insufficient-material outcomes.
- Add a lossless `ChessPositionSnapshot` for rollback. FEN remains the
  interchange format for analysis, not the rollback format.
- Add UCI and FEN codecs immediately; add mainline PGN/SAN replay before
  puzzles and imported-game review.
- Refactor native `ChessGame` into a networking/clock controller that delegates
  chess rules to the model. Preserve its existing lobby, seating, RPC, and
  broadcast authority.
- Introduce `IChessMatchController` for the shared board UI: board state,
  orientation, local color, clocks, pending state, outcome, legal moves, and
  asynchronous move submission.
- Remove direct `ChessGame`, `Connection.Local`, and RPC assumptions from the
  reusable board presentation. Keep provider-specific action panels separate.

### 2. Add mode, authentication, and runtime isolation

- Replace the main action list with a persisted `Kachess | Lichess` selector.
  First launch defaults to Kachess.
- Kachess mode retains Create/Join unchanged.
- Lichess mode shows:
  - masked token input and Connect/Disconnect;
  - Play Lichess after successful authentication;
  - Puzzles and Analysis only after their implementation passes land.
- Validate tokens through `SetToken` and `TestTokensAsync`; require
  `board:play`. Display the returned account ID and permission failures.
- Keep the token only in memory. Never place it in a scene property, RPC,
  sync field, settings file, log, or exception text.
- Host `LichessRuntime` on a locally created object flagged
  `DontDestroyOnLoad | NotNetworked | NotSaved`; it owns the client, account,
  active-game identity, and cancellation lifecycle.
  [S&box documents these object flags here.](https://sbox.game/api/Sandbox.GameObject/Flags)
- Persist only mode and non-secret seek preferences through `FileSystem.Data`.
  [S&box file-system documentation.](https://sbox.game/dev/doc/assets/file-system)
- Switching back to Kachess cancels an active seek but retains the validated
  token for the process session. Explicit Disconnect or application shutdown
  clears it.
- Keep the vendored library under `Libraries`; do not add a package reference.
  S&box automatically references installed library source.
  [S&box library documentation.](https://sbox.game/dev/doc/code/libraries)

### 3. Implement queue and live play

- Add a dedicated Lichess scene/controller with this state machine:

  `Setup → Seeking → Playing → Reconnecting → Finished`

- The scene owns queue/session streams, preventing scene-transition ownership
  races. `LichessRuntime` supplies only the authenticated client and launch
  context.
- Expose standard realtime seeks only:
  - casual or rated;
  - existing clock presets plus validated custom minutes/increment;
  - random, white, or black color.
- Omit variants, correspondence, rating range, direct challenges, and bots.
- Provide visible seeking status and Cancel. Account-stream failure cancels the
  seek and offers one-click requeue rather than risking duplicate seeks.
- After authentication, query ongoing games. Offer Resume for supported,
  standard games before opening another seek.
- `LichessGameController` implements the board adapter over
  `ChessMatchModel`:
  - local moves require the local color and become pending;
  - remote/history moves validate solely against model side-to-move;
  - snapshots include every rule field required for exact rollback.
- Treat Lichess as authoritative for clocks and termination:
  - interpolate displayed clocks between server updates;
  - never declare timeout or final result locally;
  - reconcile every `gameFull`/`gameState` update.
- Disable input while a local move awaits stream confirmation. Rejection rolls
  back, displays a recoverable error, and re-enables input after reconciliation.
- Show opponent identity, color/orientation, rated/casual state, clocks, last
  move, connection state, and terminal reason.
- Implement resign, abort, offer/accept/decline draw, and player chat. Map false
  action results to user-facing rejection without mutating local state.
- Auto-reconnect active game streams after 1, 2, 5, 10, then 15 seconds,
  continuing at 15-second intervals. Stop retries for authentication failures,
  explicit disposal, or a terminal game state.
- Unexpected scene exit retains the game ID for Resume. Normal Back prompts the
  player to stay, resign, or return while preserving the resumable game.
- Marshal stream callbacks into a controller-owned main-thread queue before
  touching components or Razor state.

### 4. Subsequent feature passes

- **Daily/random puzzles**
  - Replay exactly `InitialPly` mainline plies from the supplied PGN.
  - Orient the board to the solver and validate against UCI solution moves.
  - Support retry, reveal, automatic opponent replies, completion, and Next.
- **Full puzzle hub, separate pass**
  - Add puzzle-ID lookup, dashboard/activity, storm statistics, and puzzle-race
    creation/linking. Do not imply local Storm/Race gameplay where the API only
    provides metadata or an external race.
- **Position analysis**
  - Accept validated FEN, the current snapshot, or an Edit Position board.
  - Expose side-to-move, castling, and en-passant controls in the editor.
  - Show 1–5 principal variations, centipawn or mate score, depth, nodes, and
    preview arrows.
- **Post-game analysis**
  - Save initial FEN, exact UCI history, metadata, and result for games played
    through Kachess.
  - Rebuild every position, evaluate sequentially with cancellation, cache by
    FEN in memory, and show progress, move list, board scrubber, score chart,
    and available principal variations.
  - Treat missing cloud evaluations as unavailable positions, not failures.
- **History review**
  - Add recent-account and game-ID import after current-game review works.
  - Preserve raw PGN, parse only its mainline into UCI, and reject unsupported
    variants or incomplete parses without partial analysis.
- Block puzzle and analysis requests while Kachess knows a Lichess game is
  active. Lichess prohibits external assistance during ongoing games.
  [Lichess fair-play policy.](https://lichess.org/page/fair-play)

## API Change Proposal Plan

### Board-play contracts

- Keep current public methods working; make additions backward-compatible.
- Add `ILichessBoardClient`, implemented by `LichessApiClient`, so queue and
  session behavior can use deterministic fakes.
- Add unstarted stream factories:
  - `CreateBoardAccountEventStreamAsync`
  - `CreateBoardGameStreamAsync`
- Existing `StreamBoard*Async` methods continue returning started streams by
  calling the new factory and `Start()`.
- Update `LichessQueue` and `LichessGameSession` to subscribe before starting,
  eliminating the first-event race.
- Add `LichessQueueState` plus `State`, `IsSeeking`, `OnStateChanged`, and a
  public token-validation operation.
- Add `LichessGameSessionOptions` with automatic reconnect delays.
- Add session properties/events for:
  - `GameFull`, `LatestState`, and `PendingLocalMove`;
  - full-state updates and draw-offer flags;
  - pending-move changes;
  - connection state and unexpected completion.
- Add `ReconnectAsync` while preserving confirmed history and pending moves.
- Capture the initial adapter snapshot. On divergent server history, restore
  that snapshot and replay the complete authoritative move list.
- Add optional `IChessInitialPositionAdapter` for non-starting FENs. Kachess
  rejects those positions in the standard-only milestone.
- Ensure every rate-limit wait and network method observes cancellation.
- Introduce sanitized `LichessApiException : HttpRequestException` carrying HTTP
  status without exposing tokens or sensitive response data.

### Analysis, puzzles, and game export

- Add cancellation-token overloads for token testing, puzzles, cloud analysis,
  ongoing games, and game export. Existing overloads forward to them.
- Validate non-empty FEN and `multiPv` range 1–5 before cloud requests.
- Extend principal variations with nullable mate score, a derived unified score,
  and parsed UCI move-list access while preserving legacy `Cp`.
- Add `GameExportOptions` and `GetGamePgnAsync` for raw, lossless PGN retrieval.
- Preserve `RawPgn` on `Game`; repair parsing across multiline movetext,
  comments, annotations, and result tokens.
- Keep SAN-to-UCI conversion in Kachess’s chess core rather than coupling the
  general API library to Kachess rules.
- Do not invent a cloud-analysis batch endpoint. Kachess orchestrates
  single-position requests sequentially through the existing rate limiter.
- Update the README with lifecycle, reconnection, event-threading, cancellation,
  initial-position, and token-safety guidance.

## Test and Acceptance Plan

- Core unit tests: every standard move type, all promotions, illegal self-check,
  every draw/terminal rule, FEN round-trip, snapshot round-trip, SAN
  disambiguation, and PGN mainline replay.
- Native regressions: two-player create/join/start/move/clock/end flow remains
  functional and invokes no Lichess code.
- Adapter tests: both colors, history replay, pending confirmation, rejection
  rollback, divergent-state rebuild, underpromotion, and exported snapshots.
- API tests with fake streams: subscription ordering, token scope, seek/cancel,
  state events, reconnect, draw flags, chat, disposal, mate evaluations,
  cancellation, and multiline PGN.
- Queue milestone manual acceptance:
  - invalid, missing-scope, and valid tokens;
  - casual/rated searches, custom clocks, every color, and cancellation;
  - full games as White and Black;
  - actions, chat, stream loss, automatic reconnect, and ongoing-game resume;
  - no token persistence, replication, or logging;
  - zero leaked streams after mode change, scene destruction, or game end.
- Puzzle acceptance: initial-ply fixtures, alternating solution lines, wrong
  move rollback, reveal, daily/random transition, and malformed PGN handling.
- Analysis acceptance: valid/invalid FEN, edit-position round-trip,
  centipawn/mate results, unavailable cloud positions, cancellation, completed
  game scrubbing, and imported-history rejection for unsupported games.

## Assumptions

- API changes default to additive compatibility.
- Active-game auto-reconnect and resume are included in the queue milestone.
- First milestone supports standard realtime games only.
- Token remains session-only; mode and seek defaults may persist.
- Kachess remains the first-launch mode.
- No disabled puzzle/analysis placeholders ship before those features work.
