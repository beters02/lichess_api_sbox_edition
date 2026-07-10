#nullable enable annotations

using LichessNET.Entities.Enumerations;

namespace LichessNET.Internal;

internal static class LichessEnumNames
{
	public static string ToApiName( Gamemode value )
	{
		return value switch
		{
			Gamemode.Bullet => "bullet",
			Gamemode.Blitz => "blitz",
			Gamemode.Rapid => "rapid",
			Gamemode.Classical => "classical",
			Gamemode.Chess960 => "chess960",
			Gamemode.KingOfTheHill => "kingOfTheHill",
			Gamemode.ThreeCheck => "threeCheck",
			Gamemode.Antichess => "antichess",
			Gamemode.Atomic => "atomic",
			Gamemode.Horde => "horde",
			Gamemode.RacingKings => "racingKings",
			Gamemode.Crazyhouse => "crazyhouse",
			_ => value.ToString().ToLowerInvariant()
		};
	}

	public static string ToApiName( ChessVariant value )
	{
		return value switch
		{
			ChessVariant.Standard => "standard",
			ChessVariant.Chess960 => "chess960",
			ChessVariant.KingOfTheHill => "kingOfTheHill",
			ChessVariant.ThreeCheck => "threeCheck",
			ChessVariant.Antichess => "antichess",
			ChessVariant.Atomic => "atomic",
			ChessVariant.Horde => "horde",
			ChessVariant.RacingKings => "racingKings",
			ChessVariant.Crazyhouse => "crazyhouse",
			ChessVariant.Storm => "puzzle",
			_ => value.ToString().ToLowerInvariant()
		};
	}

	public static string ToApiName( ChallengeDeniedReason value )
	{
		return value switch
		{
			ChallengeDeniedReason.Generic => "generic",
			ChallengeDeniedReason.Later => "later",
			ChallengeDeniedReason.TooFast => "tooFast",
			ChallengeDeniedReason.TooSlow => "tooSlow",
			ChallengeDeniedReason.TimeControl => "timeControl",
			ChallengeDeniedReason.Rated => "rated",
			ChallengeDeniedReason.Casual => "casual",
			ChallengeDeniedReason.Standard => "standard",
			ChallengeDeniedReason.Variant => "variant",
			ChallengeDeniedReason.NoBot => "noBot",
			ChallengeDeniedReason.OnlyBot => "onlyBot",
			_ => value.ToString().ToLowerInvariant()
		};
	}
}



