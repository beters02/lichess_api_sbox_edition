#nullable enable annotations

using Sandbox;
using LichessNET.API;
using LichessNET.Entities.Board;
using LichessNET.Gameplay;

public sealed class LichessBoardSmokeTest : Component
{
    [Property] public string Token { get; set; }
    [Property] public bool AutoSeek { get; set; }
    [Property] public bool Rated { get; set; }
    [Property] public float ClockMinutes { get; set; } = 3f;
    [Property] public int IncrementSeconds { get; set; }
    [Property] public string Color { get; set; } = "random";

    private LichessQueue _queue;
    private LichessGameSession _session;

    protected override async void OnStart()
    {
        if (!AutoSeek)
            return;

        if (string.IsNullOrWhiteSpace(Token))
        {
            Log.Warning("Lichess board smoke skipped: token is empty.");
            return;
        }

        try
        {
            var lichess = new LichessApiClient();
            await lichess.SetToken(Token);

            _queue = new LichessQueue(lichess);
            _queue.OnGameFound += HandleGameFound;
            _queue.OnError += (_, ex) => Log.Error("Lichess queue error: " + ex.Message);

            await _queue.StartSeekAsync(new BoardSeekOptions
            {
                Rated = Rated,
                TimeMinutes = Math.Max(1, ClockMinutes),
                IncrementSeconds = Math.Max(0, IncrementSeconds),
                Color = ParseColor(Color)
            });

            Log.Info("Lichess board seek started.");
        }
        catch (Exception ex)
        {
            Log.Error("Lichess board smoke failed: " + ex.Message);
        }
    }

    protected override async void OnDestroy()
    {
        if (_session != null)
            await _session.DisposeAsync();

        if (_queue != null)
            await _queue.DisposeAsync();
    }

    private void HandleGameFound(LichessQueue queue, LichessGameFoundEventArgs game)
    {
        Log.Info($"Lichess game found: {game.GameId} as {game.Color}");
        _ = StartSessionAsync(queue, game);
    }

    private async Task StartSessionAsync(LichessQueue queue, LichessGameFoundEventArgs game)
    {
        try
        {
            var adapter = new RecordingChessBoardAdapter();
            _session = new LichessGameSession(queue.Api, game.GameId, game.Color, adapter);
            _session.OnOpponentMove += (_, move) => Log.Info("Opponent move: " + move);
            _session.OnClockUpdate += (_, clock) => Log.Info($"Clock: W={clock.WhiteTime} B={clock.BlackTime}");
            _session.OnChatLine += (_, chat) => Log.Info($"Chat[{chat.Room}] {chat.Username}: {chat.Text}");
            _session.OnGameOver += (_, state) => Log.Info($"Game over: {state.Status}, winner={state.Winner}");
            _session.OnDesync += (_, reason) => Log.Warning("Board desync: " + reason);
            _session.OnError += (_, ex) => Log.Error("Session error: " + ex.Message);
            await _session.StartAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Failed to start Lichess board session: " + ex.Message);
        }
    }

    private static BoardColorPreference ParseColor(string color)
    {
        return color?.Trim().ToLowerInvariant() switch
        {
            "white" => BoardColorPreference.White,
            "black" => BoardColorPreference.Black,
            _ => BoardColorPreference.Random
        };
    }
}
