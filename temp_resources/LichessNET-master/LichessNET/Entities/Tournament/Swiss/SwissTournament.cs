using LichessNET.Entities.Game;

namespace LichessNET.Entities.Tournament;

public class SwissTournament
{
    public Clock Clock { get; set; }
    public string CreatedBy { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public int NbOngoing { get; set; }
    public int NbPlayers { get; set; }
    public int NbRounds { get; set; }
    public bool Rated { get; set; }
    public int Round { get; set; }
    public DateTime StartsAt { get; set; }
    public SwissTournamentStats Stats { get; set; }
    public string Status { get; set; }
    public string Variant { get; set; }
    public Verdicts Verdicts { get; set; }
}