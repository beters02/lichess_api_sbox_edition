namespace LichessNET.Entities.Puzzle.PuzzleStorm;

public class StormDay
{
    public string Id { get; set; }

    public DateOnly Date
    {
        get
        {
            return new DateOnly(
                int.Parse(Id.Split('/')[0]),
                int.Parse(Id.Split('/')[1]),
                int.Parse(Id.Split('/')[2])
            );
        }
    }

    public int Combo { get; set; }
    public int Errors { get; set; }
    public int Highest { get; set; }
    public int Moves { get; set; }
    public int Runs { get; set; }
    public int Score { get; set; }
    public int Time { get; set; }
}