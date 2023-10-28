namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelComparer : IComparer<IPanelViewModel>
{
    public static readonly PanelComparer Instance = new();

    public int Compare(IPanelViewModel? a, IPanelViewModel? b)
    {
        if (a is null) return -1;
        if (b is null) return 1;
        if (ReferenceEquals(a, b)) return 0;

        var xComparison = a.LogicalBounds.X.CompareTo(b.LogicalBounds.X);
        if (xComparison != 0) return xComparison;
        return a.LogicalBounds.Y.CompareTo(b.LogicalBounds.Y);
    }
}
