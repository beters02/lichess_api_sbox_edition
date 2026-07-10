namespace LichessNET.Entities.Social.Timeline;

public class Timeline
{
    public List<TimelineEntry> Entries { get; set; }
    public Dictionary<string, TimelineUser> Users { get; set; }
}