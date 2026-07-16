#nullable enable annotations

using System.Text;

namespace LichessNET.OAuth;

public sealed record LichessOAuthRequest(
    string AuthorizationUrl,
    string Verifier,
    string State,
    string RedirectUri);

public static class LichessOAuth
{
    public const string ClientId = "kachess.sbox.game";
    public const string RedirectUri =
        "https://localhost/kachess/oauth/callback";
    public const string Scopes = "board:play puzzle:read puzzle:write";

    public static LichessOAuthRequest CreateAuthorizationRequest(
        string? redirectUri = null)
    {
        redirectUri = string.IsNullOrWhiteSpace(redirectUri)
            ? RedirectUri
            : redirectUri.Trim();
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedRedirect)
            || (parsedRedirect.Scheme != Uri.UriSchemeHttps
                && (parsedRedirect.Scheme != Uri.UriSchemeHttp
                    || !parsedRedirect.IsLoopback)))
            throw new ArgumentException(
                "OAuth redirect must use HTTPS or loopback HTTP.");
        var verifier = Base64Url(ManagedCrypto.RandomBytes(64));
        var state = Base64Url(ManagedCrypto.RandomBytes(32));
        var challenge = Base64Url(
            ManagedCrypto.Sha256(Encoding.ASCII.GetBytes(verifier)));
        var url = "https://lichess.org/oauth"
            + "?response_type=code"
            + "&client_id=" + Uri.EscapeDataString(ClientId)
            + "&redirect_uri=" + Uri.EscapeDataString(redirectUri)
            + "&code_challenge_method=S256"
            + "&code_challenge=" + Uri.EscapeDataString(challenge)
            + "&scope=" + Uri.EscapeDataString(Scopes)
            + "&state=" + Uri.EscapeDataString(state);
        return new LichessOAuthRequest(url, verifier, state, redirectUri);
    }

    public static bool TryCaptureCallback(
        string url,
        string expectedState,
        out string code,
        out bool accessDenied,
        out string error)
    {
        return TryCaptureCallback(url, expectedState, RedirectUri,
            out code, out accessDenied, out error);
    }

    public static bool TryCaptureCallback(
        string url,
        string expectedState,
        string expectedRedirectUri,
        out string code,
        out bool accessDenied,
        out string error)
    {
        code = "";
        accessDenied = false;
        error = "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !Uri.TryCreate(expectedRedirectUri, UriKind.Absolute,
                out var expectedUri)
            || !SameRedirect(uri, expectedUri))
            return false;

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("state", out var returnedState)
            || !FixedEquals(returnedState, expectedState))
        {
            error = "Lichess login state did not match.";
            return true;
        }
        query.TryGetValue("error", out var oauthError);
        if (string.IsNullOrWhiteSpace(oauthError))
            query.TryGetValue("err", out oauthError);
        accessDenied = string.Equals(oauthError, "access_denied",
            StringComparison.OrdinalIgnoreCase);
        if (accessDenied)
            return true;
        if (!string.IsNullOrWhiteSpace(oauthError))
        {
            error = "Lichess login failed.";
            return true;
        }
        if (!query.TryGetValue("code", out code)
            || string.IsNullOrWhiteSpace(code))
            error = "Lichess returned no authorization code.";
        return true;
    }

    private static bool SameRedirect(Uri actual, Uri expected)
    {
        return string.Equals(actual.Scheme, expected.Scheme,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(actual.Host, expected.Host,
                StringComparison.OrdinalIgnoreCase)
            && actual.Port == expected.Port
            && string.Equals(actual.AbsolutePath, expected.AbsolutePath,
                StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var item in query.TrimStart('?').Split('&',
            StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            values[Uri.UnescapeDataString(pair[0])] = pair.Length == 2
                ? Uri.UnescapeDataString(pair[1].Replace("+", " "))
                : "";
        }
        return values;
    }

    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left ?? "");
        var b = Encoding.UTF8.GetBytes(right ?? "");
        try
        {
            return a.Length == b.Length
                && ManagedCrypto.FixedEquals(a, b);
        }
        finally
        {
            ManagedCrypto.Clear(a);
            ManagedCrypto.Clear(b);
        }
    }

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=')
            .Replace('+', '-').Replace('/', '_');
}

public sealed class LichessEncryptedCredential
{
    public int FormatVersion { get; set; } = 1;
    public string Salt { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string AuthenticationTag { get; set; } = "";
    public string AccountDisplayId { get; set; } = "";
}

public static class LichessCredentialStore
{
    public const int Iterations = 310000;
    private static readonly byte[] AssociatedData =
        Encoding.UTF8.GetBytes("kachess-lichess-oauth-v1");

    public static LichessEncryptedCredential Encrypt(
        string accessToken,
        string passphrase,
        string accountDisplayId)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token is required.");
        if (passphrase?.Length < 8)
            throw new ArgumentException(
                "Passphrase must contain at least eight characters.");
        var salt = ManagedCrypto.RandomBytes(16);
        var nonce = ManagedCrypto.RandomBytes(12);
        var key = ManagedCrypto.Pbkdf2(passphrase, salt, Iterations);
        var plaintext = Encoding.UTF8.GetBytes(accessToken);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        try
        {
            ManagedCrypto.GcmEncrypt(key, nonce, plaintext, AssociatedData,
                out ciphertext, out tag);
            return new LichessEncryptedCredential
            {
                Salt = Convert.ToBase64String(salt),
                Nonce = Convert.ToBase64String(nonce),
                Ciphertext = Convert.ToBase64String(ciphertext),
                AuthenticationTag = Convert.ToBase64String(tag),
                AccountDisplayId = accountDisplayId ?? ""
            };
        }
        finally
        {
            ManagedCrypto.Clear(key);
            ManagedCrypto.Clear(plaintext);
        }
    }

    public static bool TryDecrypt(
        LichessEncryptedCredential? credential,
        string passphrase,
        out string accessToken)
    {
        accessToken = "";
        if (credential?.FormatVersion != 1
            || string.IsNullOrEmpty(passphrase))
            return false;
        byte[]? key = null;
        byte[]? plaintext = null;
        try
        {
            var salt = Convert.FromBase64String(credential.Salt);
            var nonce = Convert.FromBase64String(credential.Nonce);
            var ciphertext = Convert.FromBase64String(credential.Ciphertext);
            var tag = Convert.FromBase64String(credential.AuthenticationTag);
            if (salt.Length != 16 || nonce.Length != 12 || tag.Length != 16)
                return false;
            key = ManagedCrypto.Pbkdf2(passphrase, salt, Iterations);
            if (!ManagedCrypto.GcmDecrypt(key, nonce, ciphertext,
                AssociatedData, tag, out plaintext))
                return false;
            accessToken = Encoding.UTF8.GetString(plaintext);
            return !string.IsNullOrWhiteSpace(accessToken);
        }
        catch (Exception exception) when (exception is FormatException
            or ArgumentException)
        {
            return false;
        }
        finally
        {
            if (key is not null) ManagedCrypto.Clear(key);
            if (plaintext is not null)
                ManagedCrypto.Clear(plaintext);
        }
    }
}
