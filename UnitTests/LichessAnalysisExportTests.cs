using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LichessNET.API;
using LichessNET.Entities.Analysis;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Game;

[TestClass]
public class LichessAnalysisExportTests
{
    [TestMethod]
    public void PrincipalVariationPreservesMateAndParsesUciMoves()
    {
        var variation = new PrincipalVariation
        {
            Mate = -3,
            Moves = "e2e4  e7e5\ng1f3"
        };

        Assert.AreEqual(EvaluationScoreKind.Mate, variation.Score.Kind);
        Assert.AreEqual(-3, variation.Score.Value);
        Assert.AreEqual(-3, variation.Score.Mate);
        Assert.IsNull(variation.Score.Cp);
        CollectionAssert.AreEqual(
            new[] { "e2e4", "e7e5", "g1f3" },
            variation.UciMoves.ToArray());
    }

    [TestMethod]
    public void GameParserHandlesMultilineCommentsAnnotationsAndResults()
    {
        const string pgn =
            "[Event \"Parser test\"]\r\n" +
            "[White \"Alice\"]\r\n" +
            "[Black \"Bob\"]\r\n" +
            "[Annotator \"A \\\"quote\\\"\"]\r\n" +
            "[Result \"1-0\"]\r\n" +
            "\r\n" +
            "1. e4! { [%clk 0:05:00] [%eval 0.23] } e5 $1\r\n" +
            "2. Nf3 (2. Bc4?! Nf6 (2... d6)) Nc6 ; side note\r\n" +
            "3. Bb5 a6 1-0\r\n";

        var game = Game.FromPgn(pgn);

        Assert.AreEqual(pgn, game.RawPgn);
        Assert.AreEqual("A \"quote\"", game.AdditionalData["Annotator"]);
        Assert.AreEqual("1-0", game.ResultToken);
        Assert.AreEqual(GameResult.WhiteVictory, game.Result);
        CollectionAssert.AreEqual(
            new[] { "e4", "e5", "Nf3", "Nc6", "Bb5", "a6" },
            game.Moves.Moves.Select(move => move.Notation).ToArray());
        Assert.AreEqual(0.23f, game.Moves.Moves[0].Evaluation, 0.001f);
        Assert.AreEqual(TimeSpan.FromMinutes(5), game.Moves.Moves[0].Clock);
    }

    [TestMethod]
    public void GameParserPreservesUnfinishedResult()
    {
        const string pgn = "[Result \"*\"]\n\n1. d4 d5 *";

        var game = Game.FromPgn(pgn);

        Assert.AreEqual("*", game.ResultToken);
        CollectionAssert.AreEqual(
            new[] { "d4", "d5" },
            game.Moves.Moves.Select(move => move.Notation).ToArray());
    }

    [TestMethod]
    public void GameParserPreservesExplicitBlackMoveNumber()
    {
        const string pgn = "[Result \"*\"]\n\n42... Kf7 43. Ke3 *";

        var game = Game.FromPgn(pgn);

        Assert.AreEqual(2, game.Moves.Moves.Count);
        Assert.AreEqual(42, game.Moves.Moves[0].MoveNumber);
        Assert.IsFalse(game.Moves.Moves[0].IsWhite);
        Assert.AreEqual("Kf7", game.Moves.Moves[0].Notation);
        Assert.AreEqual(43, game.Moves.Moves[1].MoveNumber);
        Assert.IsTrue(game.Moves.Moves[1].IsWhite);
    }

    // Compile-only guard for legacy default-literal calls.
    private static void CompileLegacyOverloads(LichessApiClient client)
    {
        _ = client.GetCloudEvaluationAsync("fen", default);
        _ = client.GetCloudEvaluationAsync("fen", 1, default);
        _ = client.GetOngoingGamesAsync(default);
        _ = client.GetStormDashboardAsync("user", default);
        _ = client.GetTablebaseLookupAsync("fen", default);
    }

    [TestMethod]
    public async Task CloudEvaluationValidatesBeforeSendingRequest()
    {
        var client = new LichessApiClient(false);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            client.GetCloudEvaluationAsync(
                " ",
                1,
                ChessVariant.Standard,
                CancellationToken.None));

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
            client.GetCloudEvaluationAsync(
                "8/8/8/8/8/8/8/8 w - - 0 1",
                6,
                ChessVariant.Standard,
                CancellationToken.None));
    }
}
