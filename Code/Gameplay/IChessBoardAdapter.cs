#nullable enable annotations

namespace LichessNET.Gameplay;

public interface IChessBoardAdapter
{
    bool TryApplyLocalMove(string uci);
    bool TryApplyRemoteMove(string uci);
    string ExportState();
    void ImportState(string state);
}

/// <summary>
/// Optional adapter capability for games whose gameFull event supplies a
/// non-starting FEN.
/// </summary>
public interface IChessInitialPositionAdapter
{
    bool TrySetInitialPosition(string fen);
}
