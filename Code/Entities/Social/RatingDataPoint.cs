namespace LichessNET.Entities.Social;

public class RatingDataPoint
{
    public RatingDataPoint(int year, int month, int day, int rating)
    {
        Date = new DateOnly(year, month + 1, day); // month + 1 because month starts at zero
        Rating = rating;
    }

    public DateOnly Date { get; set; }
    public int Rating { get; set; }
}