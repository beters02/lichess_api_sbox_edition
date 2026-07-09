namespace LichessNET.Entities.Stats;

public class Playtime
{

    /// <summary>
    ///     The total time played by the user
    /// </summary>
    public TimeSpan TotalSpan { get; set; }

    /// <summary>
    ///     The total time seen on TV
    /// </summary>
    public TimeSpan TvSpan { get; set; }
}
