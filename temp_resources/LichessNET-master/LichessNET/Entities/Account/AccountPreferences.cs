namespace LichessNET.Entities.Account;

/// <summary>
///     Represents the account preferences of a user on Lichess.
/// </summary>
public class AccountPreferences
{
    public Preferences Prefs { get; set; }
    public string Language { get; set; }
}