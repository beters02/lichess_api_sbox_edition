using LichessNET.Entities.Interfaces;

namespace LichessNET.Entities.Stats;

public class RunStats : IGameStats
{
    public int Runs { get; set; }
    public int Score { get; set; }
}