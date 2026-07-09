namespace LichessNET.Entities.Account.Performance;

public class PerfType
{
    public string Key { get; set; }
    public string Name { get; set; }
}

public class Highest
{
    public int Int { get; set; }
    public DateTime At { get; set; }
    public string GameId { get; set; }
}

public class BestWins
{
    public List<Result> Results { get; set; }
}

public class WorstLosses
{
    public List<Result> Results { get; set; }
}

public class Result
{
    public int OpRating { get; set; }
    public OpId OpId { get; set; }
    public DateTime At { get; set; }
    public string GameId { get; set; }
}

public class OpId
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public class Count
{
    public int All { get; set; }
    public int Rated { get; set; }
    public int Win { get; set; }
    public int Loss { get; set; }
    public int Draw { get; set; }
    public int Tour { get; set; }
    public int Berserk { get; set; }
    public double OpAvg { get; set; }
    public int Seconds { get; set; }
    public int Disconnects { get; set; }
}

public class ResultStreak
{
    public Streak Win { get; set; }
    public Streak Loss { get; set; }
}

public class Streak
{
    public Cur Cur { get; set; }
    public Max Max { get; set; }
}

public class Cur
{
    public int V { get; set; }
}

public class Max
{
    public int V { get; set; }
    public From From { get; set; }
    public To To { get; set; }
}

public class From
{
    public DateTime At { get; set; }
    public string GameId { get; set; }
}

public class To
{
    public DateTime At { get; set; }
    public string GameId { get; set; }
}

public class UserId
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
}

public class PlayStreak
{
    public Nb Nb { get; set; }
    public Time Time { get; set; }
    public DateTime LastDate { get; set; }
}

public class Nb
{
    public Cur Cur { get; set; }
    public Max Max { get; set; }
}

public class Time
{
    public Cur Cur { get; set; }
    public Max Max { get; set; }
}