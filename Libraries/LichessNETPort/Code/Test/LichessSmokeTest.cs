using Sandbox;
using LichessNET.API;
using LichessNET.Entities.Enumerations;

public sealed class LichessSmokeTest : Component
{
	protected override async void OnStart()
	{
		var lichess = new LichessApiClient();

		var puzzle = await lichess.GetDailyPuzzle();
		Log.Info( $"Daily puzzle OK: {puzzle?.Id}" );

		var status = await lichess.GetRealTimeUserStatusAsync( "thibault" );
		Log.Info( $"User status OK: {status?.Name}" );

		var leaders = await lichess.GetLeaderboardAsync( 3, Gamemode.Blitz );
		Log.Info( $"Leaderboard OK: {leaders.Count}" );

		var tb = await lichess.GetTablebaseLookupAsync(
			"7k/5K2/6Q1/8/8/8/8/8 w - - 0 1",
			ChessVariant.Standard
		);
		Log.Info( $"Tablebase OK: {tb?.Category}" );
	}
}