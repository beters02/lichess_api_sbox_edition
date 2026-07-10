namespace LichessNET.Entities.Game;

/// <summary>
/// All opponent data sent with ongoing games
/// </summary>
public class Opponent
{
    public string Id { get; set; }
    public int Rating { get; set; }
    public string Username { get; set; }
}