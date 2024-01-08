using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public record struct PanelGridState(PanelId Id, Rect Rect);

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
