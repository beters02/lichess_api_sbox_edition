#nullable enable annotations

namespace LichessNET.Entities.Enumerations;

public enum TokenPermission
{
    ReadEmail,
    ReadPreferences,
    WritePreferences,
    ReadFollows,
    WriteFollows,
    WriteMessages,
    ReadChallenges,
    WriteChallenges,
    BulkChallenges,
    WriteTournaments,
    ReadTeams,
    WriteTeams,
    ManageTeams,
    ReadPuzzleActivity,
    WritePuzzleActivity,
    WriteRaces,
    ReadStudies,
    WriteStudies,
    PlayGames,
    ReadEngines,
    ManageEngines,
}

