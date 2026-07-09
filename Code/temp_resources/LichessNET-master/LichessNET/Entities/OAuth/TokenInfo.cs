using System.Text.Json.Serialization;
using LichessNET.Converters;
using LichessNET.Entities.Enumerations;

namespace LichessNET.Entities.OAuth;

public class TokenInfo
{
    public string UserId { get; set; }
    
    [JsonConverter(typeof(PermissionJsonConverter))]
    [JsonPropertyName("scopes")]
    public List<TokenPermission> Permissions { get; set; }
    public int? Expires { get; set; }

    public bool IsAllowed(TokenPermission permission) => Permissions.Contains(permission);
    public static List<TokenPermission> GetPermissions(string permissions)
    {
        string[] permissionsList = permissions.Split(',');
        List<TokenPermission> tokenPermissions = new List<TokenPermission>();

        foreach (string permission in permissionsList)
        {
            switch (permission)
            {
                case "email:read":
                    tokenPermissions.Add(TokenPermission.ReadEmail);
                    break;
                case "preference:read":
                    tokenPermissions.Add(TokenPermission.ReadPreferences);
                    break;
                case "preference:write":
                    tokenPermissions.Add(TokenPermission.WritePreferences);
                    break;
                case "follow:read":
                    tokenPermissions.Add(TokenPermission.ReadFollows);
                    break;
                case "follow:write":
                    tokenPermissions.Add(TokenPermission.WriteFollows);
                    break;
                case "msg:write":
                    tokenPermissions.Add(TokenPermission.WriteMessages);
                    break;
                case "challenge:read":
                    tokenPermissions.Add(TokenPermission.ReadChallenges);
                    break;
                case "challenge:write":
                    tokenPermissions.Add(TokenPermission.WriteChallenges);
                    break;
                case "challenge:bulk":
                    tokenPermissions.Add(TokenPermission.BulkChallenges);
                    break;
                case "tournament:write":
                    tokenPermissions.Add(TokenPermission.WriteTournaments);
                    break;
                case "team:read":
                    tokenPermissions.Add(TokenPermission.ReadTeams);
                    break;
                case "team:write":
                    tokenPermissions.Add(TokenPermission.WriteTeams);
                    break;
                case "team:lead":
                    tokenPermissions.Add(TokenPermission.ManageTeams);
                    break;
                case "puzzle:read":
                    tokenPermissions.Add(TokenPermission.ReadPuzzleActivity);
                    break;
                case "racer:write":
                    tokenPermissions.Add(TokenPermission.WriteRaces);
                    break;
                case "study:read":
                    tokenPermissions.Add(TokenPermission.ReadStudies);
                    break;
                case "study:write":
                    tokenPermissions.Add(TokenPermission.WriteStudies);
                    break;
                case "board:play":
                    tokenPermissions.Add(TokenPermission.PlayGames);
                    break;
                case "engine:read":
                    tokenPermissions.Add(TokenPermission.ReadEngines);
                    break;
                case "engine:write":
                    tokenPermissions.Add(TokenPermission.ManageEngines);
                    break;
            }
        }

        return tokenPermissions;
    }
}