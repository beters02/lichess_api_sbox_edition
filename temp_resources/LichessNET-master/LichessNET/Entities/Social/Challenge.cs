using LichessNET.Entities.Game;

namespace LichessNET.Entities.Social;

public class Challenge
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Status { get; set; }
    public Challenger Challenger { get; set; }
    public Challenger DestUser { get; set; }
    public Variant Variant { get; set; }
    public bool Rated { get; set; }
    public string Speed { get; set; }
    public TimeControl TimeControl { get; set; }
    public string Color { get; set; }
    public string FinalColor { get; set; }
    public string Direction { get; set; }
}