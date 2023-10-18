namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelTabComparer : IComparer<IPanelTabViewModel>
{
    public static readonly IComparer<IPanelTabViewModel> Instance = new PanelTabComparer();

    public int Compare(IPanelTabViewModel? a, IPanelTabViewModel? b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        return a.Index.Value.CompareTo(b.Index.Value);
    }
}
