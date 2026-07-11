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