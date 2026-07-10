namespace LichessNET.Entities.Analysis;

public class TablebaseMove
{
    public string Uci { get; set; }
    public string San { get; set; }
    public int Dtz { get; set; }
    public int PreciseDtz { get; set; }
    public int Dtm { get; set; }
    public int? Dtw { get; set; }
    public bool Zeroing { get; set; }
    public bool Checkmate { get; set; }
    public bool Stalemate { get; set; }
    public bool VariantWin { get; set; }
    public bool VariantLoss { get; set; }
    public bool InsufficientMaterial { get; set; }
    public string Category { get; set; }
}