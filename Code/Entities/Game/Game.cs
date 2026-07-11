#nullable enable annotations

using LichessNET.Entities.Enumerations;

namespace LichessNET.Entities.Game;

public class Game
{
    private static readonly System.Text.RegularExpressions.Regex HeaderRegex = new(
        @"^\s*\[(?<key>[^\s\]]+)\s+""(?<value>(?:\\.|[^""])*)""\]\s*$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex MovePrefixRegex = new(
        @"^(?<number>\d+)\.(?<black>\.\.)?(?<move>.*)$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex InlineNagRegex = new(
        @"\$\d+",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex AnnotationRegex = new(
        @"[!?]+$",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex EvaluationRegex = new(
        @"\[%eval\s+(?<value>[+-]?\d+(?:\.\d+)?)\]",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex ClockRegex = new(
        @"\[%clk\s+(?<value>\d+:\d{2}:\d{2}(?:\.\d+)?)\]",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private sealed class MoveCursor
    {
        public int MoveNumber { get; set; } = 1;
        public bool IsWhite { get; set; } = true;
    }

    public Dictionary<string, string> AdditionalData { get; set; } =
        new Dictionary<string, string>();
    public string Event { get; set; }
    public string Url { get; set; }
    public GamePlayer White { get; set; }
    public GamePlayer Black { get; set; }
    public GameResult Result { get; set; }

    /// <summary>
    /// Exact PGN result marker, including "*" for an unfinished game.
    /// </summary>
    public string ResultToken { get; set; }

    public string Eco { get; set; }
    public string Opening { get; set; }
    public MoveSequence Moves { get; set; }

    /// <summary>
    /// Exact PGN supplied to <see cref="FromPgn"/>. It is never normalized,
    /// trimmed, or regenerated from parsed data.
    /// </summary>
    public string RawPgn { get; set; }

    /// <summary>
    /// Creates a new Game instance by parsing PGN headers and mainline SAN.
    /// Comments are retained for clock/evaluation metadata, while recursive
    /// annotation variations and numeric annotation glyphs are excluded.
    /// </summary>
    public static Game FromPgn(string pgn)
    {
        if (pgn == null)
            throw new ArgumentNullException(nameof(pgn));

        var game = new Game
        {
            RawPgn = pgn,
            White = new GamePlayer(),
            Black = new GamePlayer(),
            Moves = new MoveSequence()
        };

        var normalized = pgn.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var moveLines = new List<string>();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = index == 0 ? lines[index].TrimStart('\uFEFF') : lines[index];
            var header = HeaderRegex.Match(line);
            if (header.Success)
            {
                ApplyHeader(
                    game,
                    header.Groups["key"].Value,
                    UnescapeHeaderValue(header.Groups["value"].Value));
            }
            else
            {
                moveLines.Add(line);
            }
        }

        game.Moves.OriginalPgn = string.Join("\n", moveLines).Trim();
        ParseMovetext(game, game.Moves.OriginalPgn);
        return game;
    }

    private static void ApplyHeader(Game game, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "whiteelo":
                game.White.Rating = ParseRating(value);
                break;
            case "blackelo":
                game.Black.Rating = ParseRating(value);
                break;
            case "site":
                game.Url = value;
                break;
            case "event":
                game.Event = value;
                break;
            case "white":
                game.White.Name = value;
                break;
            case "black":
                game.Black.Name = value;
                break;
            case "result":
                TryApplyResult(game, value);
                break;
            case "eco":
                game.Eco = value;
                break;
            case "opening":
                game.Opening = value;
                break;
            default:
                game.AdditionalData[key] = value;
                break;
        }
    }

    private static int ParseRating(string value)
    {
        return int.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var rating)
            ? rating
            : 0;
    }

    private static string UnescapeHeaderValue(string value)
    {
        var result = new StringBuilder(value.Length);
        var escaped = false;

        foreach (var character in value)
        {
            if (escaped)
            {
                result.Append(character);
                escaped = false;
            }
            else if (character == '\\')
            {
                escaped = true;
            }
            else
            {
                result.Append(character);
            }
        }

        if (escaped)
            result.Append('\\');

        return result.ToString();
    }

    private static void ParseMovetext(Game game, string movetext)
    {
        var token = new StringBuilder();
        var cursor = new MoveCursor();

        for (var index = 0; index < movetext.Length;)
        {
            var character = movetext[index];

            if (char.IsWhiteSpace(character))
            {
                FlushToken(game, token, cursor);
                index++;
                continue;
            }

            if (character == '{')
            {
                FlushToken(game, token, cursor);
                var commentStart = ++index;
                while (index < movetext.Length && movetext[index] != '}')
                    index++;

                ApplyComment(game, movetext.Substring(commentStart, index - commentStart));
                if (index < movetext.Length)
                    index++;
                continue;
            }

            if (character == ';')
            {
                FlushToken(game, token, cursor);
                while (index < movetext.Length && movetext[index] != '\n')
                    index++;
                continue;
            }

            if (character == '(')
            {
                FlushToken(game, token, cursor);
                SkipVariation(movetext, ref index);
                continue;
            }

            token.Append(character);
            index++;
        }

        FlushToken(game, token, cursor);
    }

    private static void SkipVariation(string movetext, ref int index)
    {
        var depth = 0;
        var inBraceComment = false;
        var inLineComment = false;

        while (index < movetext.Length)
        {
            var character = movetext[index++];

            if (inLineComment)
            {
                if (character == '\n')
                    inLineComment = false;
                continue;
            }

            if (inBraceComment)
            {
                if (character == '}')
                    inBraceComment = false;
                continue;
            }

            if (character == ';')
            {
                inLineComment = true;
            }
            else if (character == '{')
            {
                inBraceComment = true;
            }
            else if (character == '(')
            {
                depth++;
            }
            else if (character == ')' && --depth <= 0)
            {
                return;
            }
        }
    }

    private static void FlushToken(Game game, StringBuilder token, MoveCursor cursor)
    {
        if (token.Length == 0)
            return;

        AddMoveToken(game, token.ToString(), cursor);
        token.Clear();
    }

    private static void AddMoveToken(Game game, string rawToken, MoveCursor cursor)
    {
        var token = rawToken.Trim();
        var prefix = MovePrefixRegex.Match(token);
        if (prefix.Success)
        {
            if (int.TryParse(prefix.Groups["number"].Value, out var moveNumber))
                cursor.MoveNumber = moveNumber;

            cursor.IsWhite = !prefix.Groups["black"].Success;
            token = prefix.Groups["move"].Value;
        }

        token = InlineNagRegex.Replace(token, string.Empty);
        if (string.IsNullOrWhiteSpace(token) ||
            token == "..." ||
            token == "e.p." ||
            IsAnnotationToken(token) ||
            TryApplyResult(game, token))
        {
            return;
        }

        token = AnnotationRegex.Replace(token, string.Empty);
        if (string.IsNullOrWhiteSpace(token))
            return;

        game.Moves.Moves.Add(new Move
        {
            Notation = token,
            MoveNumber = cursor.MoveNumber,
            IsWhite = cursor.IsWhite
        });

        if (cursor.IsWhite)
        {
            cursor.IsWhite = false;
        }
        else
        {
            cursor.IsWhite = true;
            cursor.MoveNumber++;
        }
    }

    private static bool IsAnnotationToken(string token)
    {
        return token == "!" ||
               token == "?" ||
               token == "!!" ||
               token == "??" ||
               token == "!?" ||
               token == "?!" ||
               token == "\u203c" ||
               token == "\u2047" ||
               token == "\u2048" ||
               token == "\u2049";
    }

    private static bool TryApplyResult(Game game, string token)
    {
        switch (token)
        {
            case "1-0":
                game.ResultToken = token;
                game.Result = GameResult.WhiteVictory;
                return true;
            case "0-1":
                game.ResultToken = token;
                game.Result = GameResult.BlackVictory;
                return true;
            case "1/2-1/2":
                game.ResultToken = token;
                game.Result = GameResult.Stalemate;
                return true;
            case "*":
                game.ResultToken = token;
                return true;
            default:
                return false;
        }
    }

    private static void ApplyComment(Game game, string comment)
    {
        if (game.Moves.Moves.Count == 0)
            return;

        var move = game.Moves.Moves[game.Moves.Moves.Count - 1];
        var evaluation = EvaluationRegex.Match(comment);
        if (evaluation.Success &&
            float.TryParse(
                evaluation.Groups["value"].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedEvaluation))
        {
            move.Evaluation = parsedEvaluation;
        }

        var clock = ClockRegex.Match(comment);
        if (clock.Success &&
            TimeSpan.TryParse(
                clock.Groups["value"].Value,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedClock))
        {
            move.Clock = parsedClock;
        }
    }
}

