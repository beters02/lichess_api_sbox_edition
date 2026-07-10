namespace LichessNET.Entities.Social;

public class Note
{
    public UserOverview From { get; set; }
    public UserOverview To { get; set; }
    public string Text { get; set; }
    public long Date { get; set; }
}