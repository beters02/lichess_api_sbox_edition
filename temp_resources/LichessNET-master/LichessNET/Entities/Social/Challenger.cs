namespace LichessNET.Entities.Social;

public class Challenger
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; }
    public bool Provisional { get; set; }
    public bool Online { get; set; }
    public int Lag { get; set; }
}