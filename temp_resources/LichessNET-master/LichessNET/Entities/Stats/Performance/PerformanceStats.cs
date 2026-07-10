using System.Text.Json.Serialization;

namespace LichessNET.Entities.Account.Performance;

public class PerformanceStats
{
    public string User { get; set; }
    public Perf Perf { get; set; }
    public int Rank { get; set; }
    public double Percentile { get; set; }
    public Stat Stat { get; set; }
}