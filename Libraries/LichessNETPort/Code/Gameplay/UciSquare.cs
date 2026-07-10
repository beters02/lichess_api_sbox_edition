#nullable enable annotations

namespace LichessNET.Gameplay;

public readonly struct UciSquare
{
    public UciSquare(string square, int x, int y)
    {
        Square = square;
        X = x;
        Y = y;
    }

    public string Square { get; }
    public int X { get; }
    public int Y { get; }

    public static (int x, int y) FromSquare(string square)
    {
        return Parse(square).ToTuple();
    }

    public static UciSquare Parse(string square)
    {
        if (!TryParse(square, out var value))
            throw new ArgumentException("Square must be valid UCI board notation, for example e2.", nameof(square));

        return value;
    }

    public static bool TryParse(string square, out UciSquare value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(square) || square.Length != 2)
            return false;

        var file = char.ToLowerInvariant(square[0]);
        var rank = square[1];
        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            return false;

        var x = file - 'a';
        var y = 8 - (rank - '0');
        value = new UciSquare(new string(new[] { file, rank }), x, y);
        return true;
    }

    public (int x, int y) ToTuple()
    {
        return (X, Y);
    }

    public override string ToString()
    {
        return Square;
    }
}

public readonly struct UciMove
{
    public UciMove(UciSquare from, UciSquare to, char? promotion)
    {
        From = from;
        To = to;
        Promotion = promotion;
    }

    public UciSquare From { get; }
    public UciSquare To { get; }
    public char? Promotion { get; }

    public static UciMove Parse(string uci)
    {
        if (!TryParse(uci, out var move))
            throw new ArgumentException("Move must be valid UCI notation, for example e2e4 or e7e8q.", nameof(uci));

        return move;
    }

    public static bool TryParse(string uci, out UciMove move)
    {
        move = default;
        if (string.IsNullOrWhiteSpace(uci))
            return false;

        uci = uci.Trim().ToLowerInvariant();
        if (uci.Length != 4 && uci.Length != 5)
            return false;

        if (!UciSquare.TryParse(uci.Substring(0, 2), out var from))
            return false;

        if (!UciSquare.TryParse(uci.Substring(2, 2), out var to))
            return false;

        char? promotion = null;
        if (uci.Length == 5)
        {
            var promotionPiece = uci[4];
            if (promotionPiece != 'q' && promotionPiece != 'r' && promotionPiece != 'b' && promotionPiece != 'n')
                return false;

            promotion = promotionPiece;
        }

        move = new UciMove(from, to, promotion);
        return true;
    }

    public override string ToString()
    {
        return Promotion.HasValue
            ? From.ToString() + To + Promotion.Value
            : From.ToString() + To;
    }
}
