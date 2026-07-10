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



