#nullable enable annotations

namespace LichessNET.Entities.Game;

/// <summary>
/// Controls the optional data returned by Lichess's single-game export endpoint.
/// Defaults match the endpoint defaults.
/// </summary>
public sealed class GameExportOptions
{
    public bool Moves { get; set; } = true;
    public bool Tags { get; set; } = true;
    public bool Clocks { get; set; } = true;
    public bool Evals { get; set; } = true;
    public bool Accuracy { get; set; }
    public bool Opening { get; set; } = true;
    public bool Division { get; set; } = true;
    public bool Literate { get; set; }

    /// <summary>
    /// Descriptive alias for <see cref="Evals"/>.
    /// </summary>
    public bool Evaluations
    {
        get => Evals;
        set => Evals = value;
    }

    internal Tuple<string, string>[] ToQueryParameters()
    {
        return new[]
        {
            Tuple.Create("moves", ToApiBoolean(Moves)),
            Tuple.Create("tags", ToApiBoolean(Tags)),
            Tuple.Create("clocks", ToApiBoolean(Clocks)),
            Tuple.Create("evals", ToApiBoolean(Evals)),
            Tuple.Create("accuracy", ToApiBoolean(Accuracy)),
            Tuple.Create("opening", ToApiBoolean(Opening)),
            Tuple.Create("division", ToApiBoolean(Division)),
            Tuple.Create("literate", ToApiBoolean(Literate))
        };
    }

    private static string ToApiBoolean(bool value)
    {
        return value ? "true" : "false";
    }
}
