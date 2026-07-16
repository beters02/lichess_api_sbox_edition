#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.OAuth;
using LichessNET.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LichessGameplayTests
{
    [TestMethod]
    public async Task QueueSubscribesBeforeStartingAccountStream()
    {
        var stream = new FakeBoardStream();
        var client = new FakeBoardClient(stream);
        stream.OnStart = () => stream.Emit(
            "{\"type\":\"gameStart\",\"game\":{\"gameId\":\"game-1\",\"color\":\"white\"}}");

        LichessGameFoundEventArgs? found = null;
        await using var queue = new LichessQueue(client);
        queue.OnGameFound += (_, game) => found = game;

        await queue.StartSeekAsync(new BoardSeekOptions());

        Assert.IsTrue(stream.Started);
        Assert.IsNotNull(found);
        Assert.AreEqual("game-1", found.GameId);
        Assert.AreEqual(LichessQueueState.GameFound, queue.State);
        Assert.IsFalse(queue.IsSeeking);
    }

    [TestMethod]
    public async Task QueueCancellationReleasesStreamAndReturnsToIdle()
    {
        var stream = new FakeBoardStream();
        var client = new FakeBoardClient(stream);
        using var cancellation = new CancellationTokenSource();
        await using var queue = new LichessQueue(client);

        await queue.StartSeekAsync(new BoardSeekOptions(), cancellation.Token);
        Assert.AreEqual(LichessQueueState.Seeking, queue.State);

        cancellation.Cancel();
        stream.Complete();

        Assert.AreEqual(LichessQueueState.Idle, queue.State);
        Assert.IsFalse(queue.IsSeeking);
        Assert.IsTrue(stream.Disposed);
    }

    [TestMethod]
    public async Task QueueWaitsForAccountStreamBeforePostingSeek()
    {
        var stream = new FakeBoardStream(false);
        var client = new FakeBoardClient(stream);
        await using var queue = new LichessQueue(client);

        var starting = queue.StartSeekAsync(new BoardSeekOptions());
        await stream.StartedTask;
        Assert.AreEqual(0, client.SeekCalls);

        stream.MarkReady();
        await starting;
        Assert.AreEqual(1, client.SeekCalls);
    }

    [TestMethod]
    public async Task QueueRejectsBlitzPublicSeek()
    {
        var stream = new FakeBoardStream();
        var client = new FakeBoardClient(stream);
        await using var queue = new LichessQueue(client);

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
            queue.StartSeekAsync(new BoardSeekOptions
            {
                TimeMinutes = 5,
                IncrementSeconds = 3
            }));
    }

    [TestMethod]
    public async Task SessionReconcilesPendingAndRebuildsDivergentHistory()
    {
        var first = new FakeBoardStream();
        var second = new FakeBoardStream();
        var client = new FakeBoardClient(new FakeBoardStream(), first, second);
        first.OnStart = () => first.Emit(GameFull("e2e4 e7e5", true));
        second.OnStart = () => second.Emit(GameFull("e2e4 e7e5 g1f3", false));

        var adapter = new RecordingChessBoardAdapter();
        await using var session = new LichessGameSession(
            client, "game-1", "white", adapter,
            new LichessGameSessionOptions { AutoReconnect = false });

        await session.StartAsync();

        Assert.AreEqual("standard", session.GameFull?.Variant);
        Assert.AreEqual("Blitz", session.GameFull?.Perf);
        Assert.IsTrue(session.WhiteOfferingDraw);
        CollectionAssert.AreEqual(
            new[] { "e2e4", "e7e5" }, new List<string>(session.MoveHistory));

        Assert.IsTrue(await session.SubmitLocalMoveAsync("g1f3"));
        Assert.AreEqual("g1f3", session.PendingLocalMove);

        await session.ReconnectAsync();

        Assert.IsNull(session.PendingLocalMove);
        CollectionAssert.AreEqual(
            new[] { "e2e4", "e7e5", "g1f3" },
            new List<string>(session.MoveHistory));

        second.Emit(
            "{\"type\":\"gameState\",\"moves\":\"d2d4\",\"status\":\"started\"}");

        CollectionAssert.AreEqual(
            new[] { "d2d4" }, new List<string>(session.MoveHistory));
        CollectionAssert.AreEqual(
            new[] { "d2d4" }, new List<string>(adapter.Moves));
    }

    [TestMethod]
    public async Task UnsupportedInitialFenBlocksLaterStateUpdates()
    {
        var stream = new FakeBoardStream();
        var client = new FakeBoardClient(new FakeBoardStream(), stream);
        stream.OnStart = () => stream.Emit(GameFull("e2e4", false, "8/8/8/8/8/8/8/8 w - - 0 1"));

        var adapter = new RecordingChessBoardAdapter();
        await using var session = new LichessGameSession(
            client, "game-1", "white", adapter,
            new LichessGameSessionOptions { AutoReconnect = false });

        await session.StartAsync();
        stream.Emit(
            "{\"type\":\"gameState\",\"moves\":\"e2e4 e7e5\",\"status\":\"started\"}");

        Assert.AreEqual(0, session.MoveHistory.Count);
        Assert.AreEqual(0, adapter.Moves.Count);
    }

    [TestMethod]
    public async Task AuthenticationFailureStopsAutomaticReconnect()
    {
        var first = new FakeBoardStream();
        var second = new FakeBoardStream();
        var client = new FakeBoardClient(new FakeBoardStream(), first, second);
        first.OnStart = () => first.Emit(GameFull(string.Empty, false));

        await using var session = new LichessGameSession(
            client, "game-1", "white", new RecordingChessBoardAdapter(),
            new LichessGameSessionOptions
            {
                AutoReconnect = true,
                ReconnectDelays = new[] { TimeSpan.Zero }
            });

        await session.StartAsync();
        first.Fail(new LichessApiException(HttpStatusCode.Unauthorized));
        first.Complete();

        Assert.IsFalse(second.Started);
        Assert.AreEqual(LichessGameConnectionState.Disconnected, session.ConnectionState);
    }

    [TestMethod]
    public async Task ZeroDelayReconnectStartsOnlyOneReplacementStream()
    {
        var first = new FakeBoardStream();
        var second = new FakeBoardStream();
        var client = new FakeBoardClient(new FakeBoardStream(), first, second);
        first.OnStart = () => first.Emit(GameFull(string.Empty, false));
        second.OnStart = () => second.Emit(GameFull(string.Empty, false));

        await using var session = new LichessGameSession(
            client, "game-1", "white", new RecordingChessBoardAdapter(),
            new LichessGameSessionOptions
            {
                AutoReconnect = true,
                ReconnectDelays = new[] { TimeSpan.Zero }
            });

        var unexpectedCompletions = 0;
        session.OnUnexpectedCompletion += _ => unexpectedCompletions++;

        await session.StartAsync();
        first.Complete();

        Assert.IsTrue(second.Started);
        Assert.AreEqual(1, unexpectedCompletions);
        Assert.AreEqual(LichessGameConnectionState.Connected, session.ConnectionState);
    }

    private static string GameFull(string moves, bool whiteDraw,
        string initialFen = "startpos")
    {
        return "{" +
               "\"type\":\"gameFull\"," +
               "\"id\":\"game-1\"," +
               "\"initialFen\":\"" + initialFen + "\"," +
               "\"variant\":{\"key\":\"standard\",\"name\":\"Standard\"}," +
               "\"speed\":\"blitz\"," +
               "\"perf\":{\"name\":\"Blitz\"}," +
               "\"state\":{" +
               "\"type\":\"gameState\"," +
               "\"moves\":\"" + moves + "\"," +
               "\"status\":\"started\"," +
               "\"wdraw\":" + whiteDraw.ToString().ToLowerInvariant() +
               "}}";
    }

    private sealed class FakeBoardStream : ILichessBoardEventStream
    {
        private readonly TaskCompletionSource<bool> _ready = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _started = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeBoardStream(bool ready = true)
        {
            if (ready)
                _ready.TrySetResult(true);
        }

        public event Action<ILichessBoardEventStream, JsonElement>? LineReceived;
        public event Action<ILichessBoardEventStream, Exception>? ErrorReceived;
        public event Action<ILichessBoardEventStream>? Completed;

        public Action? OnStart { get; set; }
        public bool Started { get; private set; }
        public bool Disposed { get; private set; }
        public Task Completion => Task.CompletedTask;
        public Task Ready => _ready.Task;
        public Task StartedTask => _started.Task;

        public void Start()
        {
            Started = true;
            _started.TrySetResult(true);
            OnStart?.Invoke();
        }

        public void MarkReady() => _ready.TrySetResult(true);

        public void Emit(string json)
        {
            var element = JsonSerializer.Deserialize<JsonElement>(json);
            LineReceived?.Invoke(this, element);
        }

        public void Fail(Exception exception)
        {
            ErrorReceived?.Invoke(this, exception);
        }

        public void Complete()
        {
            Completed?.Invoke(this);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeBoardClient : ILichessBoardClient
    {
        private readonly Queue<ILichessBoardEventStream> _gameStreams = new();
        private readonly ILichessBoardEventStream _accountStream;

        public int SeekCalls { get; private set; }

        public FakeBoardClient(ILichessBoardEventStream accountStream,
            params ILichessBoardEventStream[] gameStreams)
        {
            _accountStream = accountStream;
            foreach (var stream in gameStreams)
                _gameStreams.Enqueue(stream);
        }

        public string? GetToken() => "test-token";

        public Task<Dictionary<string, TokenInfo?>> TestTokensAsync(
            List<string> tokens, CancellationToken cancellationToken = default)
        {
            var info = new TokenInfo
            {
                Permissions = new List<TokenPermission> { TokenPermission.PlayGames }
            };
            return Task.FromResult(new Dictionary<string, TokenInfo?>
            {
                ["test-token"] = info
            });
        }

        public Task CreateBoardSeekAsync(BoardSeekOptions options,
            CancellationToken cancellationToken = default)
        {
            SeekCalls++;
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

        public Task<ILichessBoardEventStream> CreateBoardAccountEventStreamAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accountStream);
        }

        public Task<ILichessBoardEventStream> CreateBoardGameStreamAsync(
            string gameId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_gameStreams.Dequeue());
        }

        public Task<bool> MakeBoardMoveAsync(string gameId, string uci,
            bool offerDraw = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AbortBoardGameAsync(string gameId,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> ResignBoardGameAsync(string gameId,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> HandleDrawOfferAsync(string gameId, bool accept,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> SendBoardChatAsync(string gameId, string text,
            BoardChatRoom room = BoardChatRoom.Player,
            CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
