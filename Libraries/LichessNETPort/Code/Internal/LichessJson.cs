namespace LichessNET.Internal;

internal static class LichessJson
{
	public static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters =
		{
			new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ),
			new LichessNET.Converters.GameStatsConverter(),
			new LichessNET.Converters.MillisecondUnixConverter(),
			new LichessNET.Converters.PermissionJsonConverter(),
			new LichessNET.Converters.SecondsToTimeSpanConverter(),
			new LichessNET.Entities.Social.Timeline.TimelineEventDataConverter()
		}
	};

	public static T Deserialize<T>( string json )
	{
		return JsonSerializer.Deserialize<T>( json, Options );
	}

	public static List<T> DeserializeLines<T>( string content )
	{
		var result = new List<T>();
		foreach ( var line in content.Split( '\n' ) )
		{
			if ( string.IsNullOrWhiteSpace( line ) )
				continue;

			var value = Deserialize<T>( line.Trim() );
			if ( value != null )
				result.Add( value );
		}

		return result;
	}

	public static JsonElement Parse( string json )
	{
		using var document = JsonDocument.Parse( json );
		return document.RootElement.Clone();
	}

	public static T Get<T>( this JsonElement element, string propertyName )
	{
		return element.TryGetProperty( propertyName, out var property )
			? property.Deserialize<T>( Options )
			: default;
	}
}





