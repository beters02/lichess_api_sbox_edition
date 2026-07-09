namespace LichessNET.Entities.Tournament;

public class Verdicts
{
    public bool Accepted { get; set; }
    public List<VerdictEntity> List { get; set; }
}

public class VerdictEntity
{
    public string Condition { get; set; }
    public string Verdict { get; set; }
}