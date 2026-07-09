namespace LichessNET.Entities.Analysis;

public class PositionEvaluation
{
    public int Depth { get; set; }
    public string Fen { get; set; }
    public int Knodes { get; set; }
    public List<PrincipalVariation> Pvs { get; set; }
}