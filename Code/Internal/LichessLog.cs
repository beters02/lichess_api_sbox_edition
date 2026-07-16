#nullable enable annotations

namespace LichessNET.Internal;

internal sealed class LichessLog
{
	private readonly string _name;
	private readonly bool _enabled;

	public LichessLog( string name, bool enabled = true )
	{
		_name = name;
		_enabled = enabled;
	}

	public void Debug( string message )
	{
		if ( _enabled )
			Log.Info( $"[{_name}] {message}" );
	}

	public void Request( string method, string uri,
		IReadOnlyDictionary<string, string> headers = null,
		HttpContent content = null )
	{
		if ( !_enabled )
			return;

		Debug( $"HTTP request: {method ?? "GET"} {SanitizeUri(uri)}" );
		if ( headers is not null )
		{
			foreach ( var header in headers.OrderBy( pair => pair.Key ) )
			{
				var value = header.Key.Equals(
					"Authorization", StringComparison.OrdinalIgnoreCase )
					? "[redacted]"
					: header.Value;
				Debug( $"HTTP header: {header.Key}: {value}" );
			}
		}

		if ( content is null )
			return;
		foreach ( var header in content.Headers.OrderBy( pair => pair.Key ) )
			Debug( $"HTTP header: {header.Key}: {string.Join(",", header.Value)}" );
	}

	private static string SanitizeUri( string uri )
	{
		if ( string.IsNullOrWhiteSpace( uri ) )
			return "[unavailable]";

		try
		{
			var builder = new UriBuilder( uri );
			var query = builder.Query.TrimStart( '?' );
			if ( string.IsNullOrWhiteSpace( query ) )
				return uri;

			var sanitized = query.Split(
				'&', StringSplitOptions.RemoveEmptyEntries ).Select( part =>
			{
				var pieces = part.Split( '=', 2 );
				var key = Uri.UnescapeDataString( pieces[0] );
				return key.Contains( "token", StringComparison.OrdinalIgnoreCase )
					? Uri.EscapeDataString( key ) + "=[redacted]"
					: part;
			} );
			builder.Query = string.Join( "&", sanitized );
			return builder.Uri.ToString();
		}
		catch
		{
			return "[unavailable]";
		}
	}

	public void Information( string message )
	{
		if ( _enabled )
			Log.Info( $"[{_name}] {message}" );
	}

	public void Warning( string message )
	{
		if ( _enabled )
			Log.Warning( $"[{_name}] {message}" );
	}

	public void Error( string message )
	{
		if ( _enabled )
			Log.Error( $"[{_name}] {message}" );
	}
}



