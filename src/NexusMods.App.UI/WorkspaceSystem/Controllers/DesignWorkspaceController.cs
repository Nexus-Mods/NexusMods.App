using Avalonia.Media;

namespace NexusMods.App.UI.WorkspaceSystem;

public class DesignWorkspaceController : IWorkspaceController
{
    public static readonly IWorkspaceController Instance = new DesignWorkspaceController();

    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior) { }

    public void OpenPage(WorkspaceId workspaceId, PageData pageData, OpenPageBehavior behavior) { }

    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId) { }

    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose) { }

    public void SetTabTitle(string title, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId) { }

    public void SetIcon(IImage? icon, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId) { }
}
