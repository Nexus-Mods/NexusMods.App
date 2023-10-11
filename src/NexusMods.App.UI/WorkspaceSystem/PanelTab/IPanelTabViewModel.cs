namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelTabViewModel : IViewModelInterface
{
    public PanelTabId Id { get; }

    public PanelTabIndex Index { get; }

    public IPanelTabHeaderViewModel Header { get; }

    public IViewModel? Contents { get; set; }
}
