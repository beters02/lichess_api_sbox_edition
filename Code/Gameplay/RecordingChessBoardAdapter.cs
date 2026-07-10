namespace LichessNET.Gameplay;

public sealed class RecordingChessBoardAdapter : IChessBoardAdapter
{
    private readonly List<string> _moves = new();

    public IReadOnlyList<string> Moves => _moves;

    public bool TryApplyLocalMove(string uci)
    {
        return TryRecordMove(uci);
    }

    public bool TryApplyRemoteMove(string uci)
    {
        return TryRecordMove(uci);
    }

    public string ExportState()
    {
        return string.Join(" ", _moves);
    }

    public void ImportState(string state)
    {
        _moves.Clear();
        if (string.IsNullOrWhiteSpace(state))
            return;

        foreach (var move in state.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (UciMove.TryParse(move, out var parsed))
                _moves.Add(parsed.ToString());
        }
    }

    private bool TryRecordMove(string uci)
    {
        if (!UciMove.TryParse(uci, out var move))
            return false;

        _moves.Add(move.ToString());
        return true;
    }
}
