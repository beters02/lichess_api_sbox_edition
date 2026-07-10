namespace LichessNET.Entities.Social.Stream;

public class LiveStreamer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public bool Patron { get; set; }
    public Stream Stream { get; set; }
    public Streamer Streamer { get; set; }
}