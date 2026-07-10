using LichessNET.Entities.Game;

namespace LichessNET.Entities.Tournament.Arena;

public class ArenaTournament
{
    public string Id { get; set; }
    public string CreatedBy { get; set; }
    public string System { get; set; }
    public int Minutes { get; set; }
    public Clock Clock { get; set; }
    public bool Rated { get; set; }
    public string FullName { get; set; }
    public int NbPlayers { get; set; }
    public Variant Variant { get; set; }
    public long StartsAt { get; set; }
    public long FinishesAt { get; set; }
    public int Status { get; set; }
    public ArenaPerf Perf { get; set; }
    public int SecondsToStart { get; set; }
    public MinRatedGames MinRatedGames { get; set; }
    public Schedule Schedule { get; set; }
}