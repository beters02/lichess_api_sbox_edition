using System.Text.Json;
using LichessNET.API;
using LichessNET.Database;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Game;

var client = new LichessApiClient();
client.SetToken("...");
var database = new DatabaseClient();

for (int year = 2013; year < 2025; year++)
{
    for (int month = 1; month <= 12; month++)
    {
        string folder = "F:/lichess/" + year;
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        await database.DownloadMonthlyDatabase(year, month, $"F:/lichess/{year}/lichess_db_{year}-{month}", false);
    }
}