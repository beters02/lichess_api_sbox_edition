using Newtonsoft.Json;

namespace LichessNET.Entities.Social.Timeline;

public class TimelineEntry
{
    public string Type { get; set; }

    [JsonConverter(typeof(TimelineEventDataConverter))]
    public TimelineEventData Data { get; set; }

    public long Date { get; set; }

    public DateTime EventTime => DateTimeOffset.FromUnixTimeMilliseconds(Date).DateTime;
}