#nullable enable annotations

namespace LichessNET.Entities.Analysis;

/// <summary>
/// Identifies how a cloud-analysis score should be interpreted.
/// </summary>
public enum EvaluationScoreKind
{
    Centipawns,
    Mate
}

/// <summary>
/// A cloud-analysis score that preserves whether its signed value represents
/// centipawns or moves to mate.
/// </summary>
public sealed class EvaluationScore
{
    private EvaluationScore(EvaluationScoreKind kind, int value)
    {
        Kind = kind;
        Value = value;
    }

    public EvaluationScoreKind Kind { get; }
    public int Value { get; }
    public bool IsMate => Kind == EvaluationScoreKind.Mate;
    public bool IsCentipawn => Kind == EvaluationScoreKind.Centipawns;
    public int? Cp => IsCentipawn ? Value : null;
    public int? Centipawns => Cp;
    public int? Mate => IsMate ? Value : null;

    public static EvaluationScore FromCentipawns(int centipawns)
    {
        return new EvaluationScore(EvaluationScoreKind.Centipawns, centipawns);
    }

    public static EvaluationScore FromMate(int movesToMate)
    {
        return new EvaluationScore(EvaluationScoreKind.Mate, movesToMate);
    }

    public override string ToString()
    {
        return IsMate ? $"Mate {Value:+#;-#;0}" : $"{Value:+#;-#;0} cp";
    }
}
