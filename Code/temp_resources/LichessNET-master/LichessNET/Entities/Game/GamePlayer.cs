using LichessNET.Entities.Enumerations;

namespace LichessNET.Entities.Game;

/// <summary>
/// Represents a player in a chess game, including their name, title, and rating.
/// This class will be used when parsing PGN Files
/// </summary>
public class GamePlayer
{
    public string Name { get; set; }

    //TODO: Add serialization for Title
    public Title? Title { get; set; }
    public int Rating { get; set; }
}