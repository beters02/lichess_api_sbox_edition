#nullable enable annotations

using LichessNET.OAuth;
using LichessNET.Converters;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.OAuth;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class OAuthSecurityTests
{
    [TestMethod]
    public void ManagedCryptoMatchesPublishedVectors()
    {
        CollectionAssert.AreEqual(
            Convert.FromHexString(
                "ba7816bf8f01cfea414140de5dae2223" +
                "b00361a396177a9cb410ff61f20015ad"),
            ManagedCrypto.Sha256(Encoding.ASCII.GetBytes("abc")));
        CollectionAssert.AreEqual(
            Convert.FromHexString(
                "120fb6cffcf8b32c43e7225256c4f837" +
                "a86548c92ccc35480805987cb70be17b"),
            ManagedCrypto.Pbkdf2("password", Encoding.ASCII.GetBytes("salt"), 1));

        var key = new byte[32];
        var nonce = new byte[12];
        var plaintext = new byte[16];
        ManagedCrypto.GcmEncrypt(key, nonce, plaintext, Array.Empty<byte>(),
            out var ciphertext, out var tag);
        CollectionAssert.AreEqual(
            Convert.FromHexString("cea7403d4d606b6e074ec5d3baf39d18"),
            ciphertext);
        CollectionAssert.AreEqual(
            Convert.FromHexString("d0d1c8a799996bf0265b98b5d48ab919"), tag);
    }

    [TestMethod]
    public void AuthorizationRequestUsesPkceAndRejectsStateMismatch()
    {
        var request = LichessOAuth.CreateAuthorizationRequest();
        StringAssert.Contains(request.AuthorizationUrl,
            "client_id=kachess.sbox.game");
        StringAssert.Contains(request.AuthorizationUrl,
            "code_challenge_method=S256");
        StringAssert.Contains(request.AuthorizationUrl,
            "board%3Aplay%20puzzle%3Aread%20puzzle%3Awrite");
        Assert.IsTrue(request.Verifier.Length is >= 43 and <= 128);
        Assert.AreEqual(LichessOAuth.RedirectUri, request.RedirectUri);

        Assert.IsTrue(LichessOAuth.TryCaptureCallback(
            LichessOAuth.RedirectUri + "?code=secret&state=wrong",
            request.State,
            out var code,
            out var denied,
            out var error));
        Assert.AreEqual("", code);
        Assert.IsFalse(denied);
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public void LoopbackHttpCallbackMatchesExactPortAndPath()
    {
        const string redirect =
            "http://localhost:8080/kachess/oauth/callback";
        var request = LichessOAuth.CreateAuthorizationRequest(redirect);
        Assert.IsTrue(LichessOAuth.TryCaptureCallback(
            redirect + "?code=secret&state=" + request.State,
            request.State,
            request.RedirectUri,
            out var code,
            out _,
            out var error));
        Assert.AreEqual("secret", code);
        Assert.AreEqual("", error);
        Assert.IsFalse(LichessOAuth.TryCaptureCallback(
            "http://localhost:8443/kachess/oauth/callback?code=secret&state="
                + request.State,
            request.State,
            request.RedirectUri,
            out _,
            out _,
            out _));
    }

    [TestMethod]
    public void TokenInfoAcceptsMillisecondExpiryAndPuzzleWriteScope()
    {
        const string json = "{\"entry\":{\"userId\":\"player\","
            + "\"scopes\":\"board:play,puzzle:read,puzzle:write\","
            + "\"expires\":1750000000000}}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new PermissionJsonConverter());
        var result = JsonSerializer.Deserialize<
            Dictionary<string, TokenInfo>>(json, options);
        var info = result["entry"];
        Assert.AreEqual(1750000000000L, info.Expires);
        CollectionAssert.Contains(
            info.Permissions,
            TokenPermission.WritePuzzleActivity);
    }

}
