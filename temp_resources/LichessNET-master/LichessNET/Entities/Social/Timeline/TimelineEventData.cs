namespace LichessNET.Entities.Social.Timeline
{
    public abstract class TimelineEventData
    {
    }

    public class FollowEventData : TimelineEventData
    {
        public string U1 { get; set; }
        public string U2 { get; set; }
    }

    public class UnknownEventData : TimelineEventData
    {
    }

    public class TeamJoinEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string TeamId { get; set; }
    }

    public class TeamCreateEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string TeamId { get; set; }
    }

    public class ForumPostEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string TopicId { get; set; }
        public string TopicName { get; set; }
        public string PostId { get; set; }
    }

    public class UblogPostEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
    }

    public class TourJoinEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string TourId { get; set; }
        public string TourName { get; set; }
    }

    public class GameEndEventData : TimelineEventData
    {
        public string FullId { get; set; }
        public string Perf { get; set; }
        public string Opponent { get; set; }
        public bool Win { get; set; }
    }

    public class SimulCreateEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string SimulId { get; set; }
        public string SimulName { get; set; }
    }

    public class SimulJoinEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string SimulId { get; set; }
        public string SimulName { get; set; }
    }

    public class StudyLikeEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string StudyId { get; set; }
        public string StudyName { get; set; }
    }

    public class PlanStartEventData : TimelineEventData
    {
        public string UserId { get; set; }
    }

    public class PlanRenewEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public int Months { get; set; }
    }

    public class BlogPostEventData : TimelineEventData
    {
        public string Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
    }

    public class UblogPostLikeEventData : TimelineEventData
    {
        public string UserId { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public class StreamStartEventData : TimelineEventData
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}