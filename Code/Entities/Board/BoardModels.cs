#nullable enable annotations

using System.Globalization;
using LichessNET.Entities.Enumerations;
using LichessNET.Extensions;

namespace LichessNET.Entities.Board;

public enum BoardColorPreference
{
    Random,
    White,
    Black
}

public enum BoardChatRoom
{
    Player,
    Spectator
}

public sealed class BoardSeekOptions
{
    public bool Rated { get; set; }
    public double TimeMinutes { get; set; } = 10;
    public int IncrementSeconds { get; set; }
    public int? DaysPerTurn { get; set; }
    public BoardColorPreference Color { get; set; } = BoardColorPreference.Random;
    public ChessVariant Variant { get; set; } = ChessVariant.Standard;
    public string RatingRange { get; set; }

    internal Dictionary<string, string> ToFormData()
    {
        Validate();

        var form = new Dictionary<string, string>
        {
            ["rated"] = Rated.ToString().ToLowerInvariant(),
            ["variant"] = Variant.ToApiName(),
            ["ratingRange"] = RatingRange ?? string.Empty,
            ["color"] = ToApiName(Color)
        };

        if (DaysPerTurn.HasValue)
        {
            form["days"] = DaysPerTurn.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            form["time"] = TimeMinutes.ToString(CultureInfo.InvariantCulture);
            form["increment"] = IncrementSeconds.ToString(CultureInfo.InvariantCulture);
        }


        return form;
    }

    private void Validate()
    {
        if (DaysPerTurn.HasValue)
        {
            if (DaysPerTurn.Value < 1 || DaysPerTurn.Value > 14)
                throw new ArgumentOutOfRangeException(nameof(DaysPerTurn), "Days per turn must be between 1 and 14.");

            return;
        }

        if (TimeMinutes <= 0 || TimeMinutes > 180)
            throw new ArgumentOutOfRangeException(nameof(TimeMinutes), "Clock time must be between 0 and 180 minutes.");

        if (IncrementSeconds < 0 || IncrementSeconds > 180)
            throw new ArgumentOutOfRangeException(nameof(IncrementSeconds), "Increment must be between 0 and 180 seconds.");

        var estimatedDurationSeconds = TimeMinutes * 60 + IncrementSeconds * 40;
        if (estimatedDurationSeconds < 480)
        {
            throw new ArgumentOutOfRangeException(nameof(TimeMinutes),
                "Board API public seeks require a Rapid or Classical clock.");
        }
    }

    private static string ToApiName(BoardColorPreference color)
    {
        return color switch
        {
            BoardColorPreference.White => "white",
            BoardColorPreference.Black => "black",
            _ => "random"
        };
    }
}

public sealed class BoardGameInfo
{
    [JsonPropertyName("gameId")]
    public string GameId { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("fullId")]
    public string FullId { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; }

    [JsonPropertyName("fen")]
    public string Fen { get; set; }

    [JsonPropertyName("hasMoved")]
    public bool? HasMoved { get; set; }

    [JsonPropertyName("isMyTurn")]
    public bool? IsMyTurn { get; set; }

    [JsonPropertyName("lastMove")]
    public string LastMove { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("variant")]
    [JsonConverter(typeof(BoardObjectNameConverter))]
    public string Variant { get; set; }

    [JsonPropertyName("speed")]
    public string Speed { get; set; }

    [JsonPropertyName("perf")]
    [JsonConverter(typeof(BoardObjectNameConverter))]
    public string Perf { get; set; }

    [JsonPropertyName("rated")]
    public bool? Rated { get; set; }

    [JsonPropertyName("opponent")]
    public BoardPlayerInfo Opponent { get; set; }

    [JsonPropertyName("white")]
    public BoardPlayerInfo White { get; set; }

    [JsonPropertyName("black")]
    public BoardPlayerInfo Black { get; set; }

    [JsonPropertyName("state")]
    public BoardGameState State { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; set; }

    public string BestGameId => !string.IsNullOrWhiteSpace(GameId) ? GameId : Id;
}

public sealed class BoardPlayerInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("provisional")]
    public bool? Provisional { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; set; }
}

public sealed class BoardClockState
{
    public int? WhiteTimeMilliseconds { get; set; }
    public int? BlackTimeMilliseconds { get; set; }
    public int? WhiteIncrementMilliseconds { get; set; }
    public int? BlackIncrementMilliseconds { get; set; }

    public TimeSpan? WhiteTime => WhiteTimeMilliseconds.HasValue
        ? TimeSpan.FromMilliseconds(WhiteTimeMilliseconds.Value)
        : null;

    public TimeSpan? BlackTime => BlackTimeMilliseconds.HasValue
        ? TimeSpan.FromMilliseconds(BlackTimeMilliseconds.Value)
        : null;

    public static BoardClockState FromState(BoardGameState state)
    {
        return new BoardClockState
        {
            WhiteTimeMilliseconds = state?.WhiteTimeMilliseconds,
            BlackTimeMilliseconds = state?.BlackTimeMilliseconds,
            WhiteIncrementMilliseconds = state?.WhiteIncrementMilliseconds,
            BlackIncrementMilliseconds = state?.BlackIncrementMilliseconds
        };
    }
}

public sealed class BoardGameState
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("moves")]
    public string Moves { get; set; }

    [JsonPropertyName("wtime")]
    public int? WhiteTimeMilliseconds { get; set; }

    [JsonPropertyName("btime")]
    public int? BlackTimeMilliseconds { get; set; }

    [JsonPropertyName("winc")]
    public int? WhiteIncrementMilliseconds { get; set; }

    [JsonPropertyName("binc")]
    public int? BlackIncrementMilliseconds { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("winner")]
    public string Winner { get; set; }

    [JsonPropertyName("wdraw")]
    public bool? WhiteOfferingDraw { get; set; }

    [JsonPropertyName("bdraw")]
    public bool? BlackOfferingDraw { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; set; }

    public BoardClockState Clock => BoardClockState.FromState(this);

    public bool IsFinished => !string.IsNullOrWhiteSpace(Status)
                              && !Status.Equals("started", StringComparison.OrdinalIgnoreCase)
                              && !Status.Equals("created", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> MoveList => BoardEventParser.SplitMoves(Moves);
}

public sealed class BoardGameStartEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("game")]
    public BoardGameInfo Game { get; set; }
}

public sealed class BoardGameFinishEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("game")]
    public BoardGameInfo Game { get; set; }
}

public sealed class BoardGameFullEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("initialFen")]
    public string InitialFen { get; set; }

    [JsonPropertyName("rated")]
    public bool? Rated { get; set; }

    [JsonPropertyName("variant")]
    [JsonConverter(typeof(BoardObjectNameConverter))]
    public string Variant { get; set; }

    [JsonPropertyName("speed")]
    public string Speed { get; set; }

    [JsonPropertyName("perf")]
    [JsonConverter(typeof(BoardObjectNameConverter))]
    public string Perf { get; set; }

    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("white")]
    public BoardPlayerInfo White { get; set; }

    [JsonPropertyName("black")]
    public BoardPlayerInfo Black { get; set; }

    [JsonPropertyName("state")]
    public BoardGameState State { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Extra { get; set; }
}

public sealed class BoardChatLineEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("room")]
    public string Room { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public static class BoardEventParser
{
    public static string GetEventType(JsonElement element)
    {
        return element.TryGetProperty("type", out var type)
            ? type.GetString()
            : string.Empty;
    }

    public static BoardGameStartEvent ParseGameStart(JsonElement element)
    {
        return element.Deserialize<BoardGameStartEvent>(LichessJson.Options);
    }

    public static BoardGameFinishEvent ParseGameFinish(JsonElement element)
    {
        return element.Deserialize<BoardGameFinishEvent>(LichessJson.Options);
    }

    public static BoardGameFullEvent ParseGameFull(JsonElement element)
    {
        return element.Deserialize<BoardGameFullEvent>(LichessJson.Options);
    }

    public static BoardGameState ParseGameState(JsonElement element)
    {
        return element.Deserialize<BoardGameState>(LichessJson.Options);
    }

    public static BoardChatLineEvent ParseChatLine(JsonElement element)
    {
        return element.Deserialize<BoardChatLineEvent>(LichessJson.Options);
    }

    public static IReadOnlyList<string> SplitMoves(string moves)
    {
        if (string.IsNullOrWhiteSpace(moves))
            return Array.Empty<string>();

        return moves.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}


/// <summary>
/// Preserves legacy string properties while accepting the object form used by
/// current Board API gameFull payloads.
/// </summary>
public sealed class BoardObjectNameConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString();

        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var document = JsonDocument.ParseValue(ref reader);
        var value = document.RootElement;
        if (value.ValueKind != JsonValueKind.Object)
            return value.ToString();

        foreach (var propertyName in new[] { "key", "name", "short" })
        {
            if (value.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return value.GetRawText();
    }

    public override void Write(Utf8JsonWriter writer, string value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
