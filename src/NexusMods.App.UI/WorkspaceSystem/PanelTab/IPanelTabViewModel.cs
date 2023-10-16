namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelTabViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets the unique identifier of the tab.
    /// </summary>
    public PanelTabId Id { get; }

    /// <summary>
    /// Gets or sets the index of the tab.
    /// </summary>
    public PanelTabIndex Index { get; set; }

    /// <summary>
    /// Gets the header view model of the tab.
    /// </summary>
    public IPanelTabHeaderViewModel Header { get; }

    /// <summary>
    /// Gets or sets the contents of tab.
    /// </summary>
    public IViewModel? Contents { get; set; }
}
