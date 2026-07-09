namespace LichessNET.Entities.Account.Performance;

public class Stat
{
    public PerfType PerfType { get; set; }
    public Highest Lowest { get; set; }
    public Highest Highest { get; set; }
    public BestWins BestWins { get; set; }
    public WorstLosses WorstLosses { get; set; }
    public Count Count { get; set; }
    public ResultStreak ResultStreak { get; set; }
    public UserId UserId { get; set; }
    public PlayStreak PlayStreak { get; set; }
}