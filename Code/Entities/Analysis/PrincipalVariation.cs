#nullable enable annotations

namespace LichessNET.Entities.Analysis;

public class PrincipalVariation
{
    /// <summary>
    /// Centipawn evaluation from White's point of view. This legacy property
    /// remains non-nullable for source compatibility; use <see cref="Score"/>
    /// to distinguish centipawn and mate evaluations.
    /// </summary>
    public int Cp { get; set; }

    /// <summary>
    /// Signed moves-to-mate evaluation from White's point of view.
    /// </summary>
    public int? Mate { get; set; }

    public string Moves { get; set; }

    /// <summary>
    /// Evaluation with its centipawn-or-mate kind preserved.
    /// </summary>
    [JsonIgnore]
    public EvaluationScore Score => Mate.HasValue
        ? EvaluationScore.FromMate(Mate.Value)
        : EvaluationScore.FromCentipawns(Cp);

    /// <summary>
    /// Principal variation split into individual UCI move tokens.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> UciMoves => string.IsNullOrWhiteSpace(Moves)
        ? Array.Empty<string>()
        : Moves.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Alias for <see cref="UciMoves"/> for consistency with board state models.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> MoveList => UciMoves;
}

