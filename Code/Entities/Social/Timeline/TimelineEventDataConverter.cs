#nullable enable annotations

namespace LichessNET.Entities.Social.Timeline;

internal class TimelineEventDataConverter : JsonConverter<TimelineEventData>
{
    public override TimelineEventData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return DeserializeData(null, document.RootElement, options);
    }

    public override void Write(Utf8JsonWriter writer, TimelineEventData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }

    internal static TimelineEventData DeserializeData(string type, JsonElement data, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(type) && data.TryGetProperty("type", out var embeddedType))
            type = embeddedType.GetString();

        if (data.TryGetProperty("data", out var nestedData))
            data = nestedData;

                TimelineEventData result = type switch
        {
            "follow" => data.Deserialize<FollowEventData>(options),
            "team-join" => data.Deserialize<TeamJoinEventData>(options),
            "team-create" => data.Deserialize<TeamCreateEventData>(options),
            "forum-post" => data.Deserialize<ForumPostEventData>(options),
            "ublog-post" => data.Deserialize<UblogPostEventData>(options),
            "tour-join" => data.Deserialize<TourJoinEventData>(options),
            "game-end" => data.Deserialize<GameEndEventData>(options),
            "simul-create" => data.Deserialize<SimulCreateEventData>(options),
            "simul-join" => data.Deserialize<SimulJoinEventData>(options),
            "study-like" => data.Deserialize<StudyLikeEventData>(options),
            "plan-start" => data.Deserialize<PlanStartEventData>(options),
            "plan-renew" => data.Deserialize<PlanRenewEventData>(options),
            "blog-post" => data.Deserialize<BlogPostEventData>(options),
            "ublog-post-like" => data.Deserialize<UblogPostLikeEventData>(options),
            "stream-start" => data.Deserialize<StreamStartEventData>(options),
            _ => null
        };

        return result ?? new UnknownEventData();
    }
}


