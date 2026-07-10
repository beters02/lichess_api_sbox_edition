namespace LichessNET.Entities.Social;

/// <summary>
///     This class contains all information of the public profile of a lichess user
/// </summary>
public class LichessProfile
{
    /// <summary>
    ///     The current country flag of the user
    /// </summary>
    public string? Flag { get; set; }

    /// <summary>
    ///     The set location of this user
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    ///     The bio of the user
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    ///     The set real name of the user
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    ///     FIDE rating of the user
    /// </summary>
    public ushort? FideRating { get; set; }

    /// <summary>
    ///     USCF rating of the user
    /// </summary>
    public ushort? UsCfRating { get; set; }

    /// <summary>
    ///     ECF rating of the user
    /// </summary>
    public ushort? EcfRating { get; set; }

    /// <summary>
    ///     CFC rating of the user
    /// </summary>
    public ushort? CfcRating { get; set; }

    /// <summary>
    ///     DSB rating of the user
    /// </summary>
    public ushort? DsbRating { get; set; }

    /// <summary>
    ///     Links mentioned in the bio of the user
    ///     Each link is seperated by \r\n
    /// </summary>
    public string? Links { get; set; }
}