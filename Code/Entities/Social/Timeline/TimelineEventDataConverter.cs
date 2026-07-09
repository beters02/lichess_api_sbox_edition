using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LichessNET.Entities.Social.Timeline;

internal class TimelineEventDataConverter : JsonConverter
{
    private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>
    {
        { "follow", typeof(FollowEventData) },
        { "team-join", typeof(TeamJoinEventData) },
        { "team-create", typeof(TeamCreateEventData) },
        { "forum-post", typeof(ForumPostEventData) },
        { "ublog-post", typeof(UblogPostEventData) },
        { "tour-join", typeof(TourJoinEventData) },
        { "game-end", typeof(GameEndEventData) },
        { "simul-create", typeof(SimulCreateEventData) },
        { "simul-join", typeof(SimulJoinEventData) },
        { "study-like", typeof(StudyLikeEventData) },
        { "plan-start", typeof(PlanStartEventData) },
        { "plan-renew", typeof(PlanRenewEventData) },
        { "blog-post", typeof(BlogPostEventData) },
        { "ublog-post-like", typeof(UblogPostLikeEventData) },
        { "stream-start", typeof(StreamStartEventData) },
        { "", typeof(UnknownEventData) }
    };

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TimelineEventData);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var type = jsonObject["type"]?.ToString();

        if (type == null || !TypeMapping.TryGetValue(type, out var targetType))
        {
            targetType = typeof(UnknownEventData);
        }

        var data = jsonObject["data"]?.ToObject(targetType, serializer);
        if (data == null)
        {
            return new UnknownEventData();
        }

        return data;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException("Writing JSON is not implemented for TimelineEventDataConverter.");
    }
}