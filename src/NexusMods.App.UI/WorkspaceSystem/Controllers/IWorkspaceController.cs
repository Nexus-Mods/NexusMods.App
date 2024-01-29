using JetBrains.Annotations;

namespace NexusMods.App.UI.WorkspaceSystem;

[PublicAPI]
public interface IWorkspaceController
{
    /// <summary>
    /// Adds a new panel to the workspace.
    /// </summary>
    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior);

    /// <summary>
    /// Opens a new page in the workspace.
    /// </summary>
    public void OpenPage(WorkspaceId workspaceId, PageData pageData, OpenPageBehavior behavior);

    /// <summary>
    /// Swaps the positions of two panels.
    /// </summary>
    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId);

    /// <summary>
    /// Closes the panel with the given ID.
    /// </summary>
    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose);
}
