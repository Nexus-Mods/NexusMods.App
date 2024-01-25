using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPageViewModelInterface : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the current workspace controller.
    /// </summary>
    public IWorkspaceController WorkspaceController { get; set; }

    /// <summary>
    /// Gets or sets the ID of the panel this page is in.
    /// </summary>
    public PanelId PanelId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tab this page is in.
    /// </summary>
    public PanelTabId TabId { get; set; }
}
