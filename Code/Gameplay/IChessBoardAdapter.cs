#nullable enable annotations

namespace LichessNET.Gameplay;

public interface IChessBoardAdapter
{
    bool TryApplyLocalMove(string uci);
    bool TryApplyRemoteMove(string uci);
    string ExportState();
    void ImportState(string state);
}
