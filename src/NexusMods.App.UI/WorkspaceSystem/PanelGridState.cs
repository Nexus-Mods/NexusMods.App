using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public record struct PanelGridState(PanelId Id, Rect Rect)
{
    /// <summary>
    /// Checks whether the current panel spans multiple columns.
    /// </summary>
    /// <remarks>
    /// The workspace allows for panels that span multiple columns e.g.,
    /// | 1 | 1 |
    /// | 2 | 3 |
    /// In this case, the panel with the ID 1 spans two columns.
    /// </remarks>
    public bool IsCrossColumn(WorkspaceGridState.ColumnInfo info)
    {
        if (Rect.X.IsCloseTo(info.X)) return !Rect.Right.IsCloseTo(info.Right());
        if (Rect.Right.IsCloseTo(info.Right())) return !Rect.X.IsCloseTo(info.X);
        return false;
    }

    /// <summary>
    /// Checks whether the current panel spans multiple rows.
    /// </summary>
    /// <remarks>
    /// The workspace allows for panels to span multiple rows e.g.,
    /// | 1 | 2 |
    /// | 1 | 3 |
    /// In this case, the panel with the ID 1 spans two rows.
    /// </remarks>
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
