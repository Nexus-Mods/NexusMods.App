using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPageViewModelInterface : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the ID of the window this page is in.
    /// </summary>
    public WindowId WindowId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the workspace this page is in.
    /// </summary>
    public WorkspaceId WorkspaceId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the panel this page is in.
    /// </summary>
    public PanelId PanelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tab this page is in.
    /// </summary>
    public PanelTabId TabId { get; set; }
}
