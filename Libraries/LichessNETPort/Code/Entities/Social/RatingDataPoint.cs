namespace LichessNET.Entities.Social;

public class RatingDataPoint
{
    public RatingDataPoint(int year, int month, int day, int rating)
    {
        Date = new DateTime(year, month + 1, day);
        Rating = rating;
    }

    public DateTime Date { get; set; }
    public int Rating { get; set; }
}
