namespace LichessNET.Entities.Account.Performance;

public class Glicko
{
    public double Rating { get; set; }
    public double Deviation { get; set; }
    public bool Provisional { get; set; }
}