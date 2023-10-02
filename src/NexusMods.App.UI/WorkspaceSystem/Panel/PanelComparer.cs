namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelComparer : IComparer<IPanelViewModel>
{
    public static readonly PanelComparer Instance = new();

    public int Compare(IPanelViewModel? x, IPanelViewModel? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        // TODO:
        return 1;
    }
}
