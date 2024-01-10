using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public record struct PanelGridState(PanelId Id, Rect Rect)
{
    public bool IsCrossColumn(WorkspaceGridState.ColumnInfo info)
    {
        if (Rect.X.IsCloseTo(info.X)) return !Rect.Right.IsCloseTo(info.Right());
        if (Rect.Right.IsCloseTo(info.Right())) return !Rect.X.IsCloseTo(info.X);
        return false;
    }

    public bool IsCrossRow(WorkspaceGridState.RowInfo info)
    {
        if (Rect.Y.IsCloseTo(info.Y)) return !Rect.Bottom.IsCloseTo(info.Bottom());
        if (Rect.Bottom.IsCloseTo(info.Bottom())) return !Rect.Y.IsCloseTo(info.Y);
        return false;
    }
}

public class PanelGridStateComparer : IComparer<PanelGridState>
{
    public static readonly IComparer<PanelGridState> Instance = new PanelGridStateComparer();

    public int Compare(PanelGridState x, PanelGridState y)
    {
        var a = x.Rect;
        var b = y.Rect;

        var xComparison = a.X.CompareTo(b.X);
        if (xComparison != 0) return xComparison;

        var yComparison = a.Y.CompareTo(b.Y);
        if (yComparison != 0) return yComparison;

        return x.Id.CompareTo(y.Id);
    }
}
