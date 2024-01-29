using Avalonia.Media;
using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public interface IWorkspaceController
{
    /// <summary>
    /// Adds a new panel to a workspace.
    /// </summary>
    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior);

    /// <summary>
    /// Opens a new page in a workspace.
    /// </summary>
    public void OpenPage(WorkspaceId workspaceId, PageData pageData, OpenPageBehavior behavior);

    /// <summary>
    /// Swaps the positions of two panels.
    /// </summary>
    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId);

    /// <summary>
    /// Closes a panel.
    /// </summary>
    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose);

    /// <summary>
    /// Sets the title of a tab.
    /// </summary>
    public void SetTabTitle(string title, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId);

    /// <summary>
    /// Sets the icon of a tab.
    /// </summary>
    public void SetIcon(IImage? icon, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId);
}
