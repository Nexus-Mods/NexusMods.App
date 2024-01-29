using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace NexusMods.App.UI.WorkspaceSystem;

internal class WorkspaceController : IWorkspaceController
{
    private readonly ILogger<WorkspaceController> _logger;
    private readonly Dictionary<WorkspaceId, WeakReference<WorkspaceViewModel>> _workspaces = new();

    public WorkspaceController(ILogger<WorkspaceController> logger)
    {
        _logger = logger;
    }

    internal void RegisterWorkspace(WorkspaceViewModel workspaceViewModel)
    {
        _workspaces.TryAdd(workspaceViewModel.Id, new WeakReference<WorkspaceViewModel>(workspaceViewModel));
    }

    internal void UnregisterWorkspace(WorkspaceViewModel workspaceViewModel)
    {
        _workspaces.Remove(workspaceViewModel.Id);
    }

    private bool TryGetWorkspace(WorkspaceId workspaceId, [NotNullWhen(true)] out WorkspaceViewModel? workspaceViewModel)
    {
        workspaceViewModel = null;
        if (!_workspaces.TryGetValue(workspaceId, out var weakReference))
        {
            _logger.LogError("Failed to find Workspace with ID {WorkspaceID}", workspaceId);
            return false;
        }

        if (!weakReference.TryGetTarget(out workspaceViewModel))
        {
            _logger.LogError("Failed to retrieve the Workspace View Model with the ID {WorkspaceID} referenced by the WeakReference", workspaceId);
            return false;
        }

        return true;
    }

    public void AddPanel(WorkspaceId workspaceId, WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.AddPanel(newWorkspaceState, behavior);
    }

    public void OpenPage(WorkspaceId workspaceId, PageData pageData, OpenPageBehavior behavior)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.OpenPage(pageData, behavior);
    }

    public void SwapPanels(WorkspaceId workspaceId, PanelId firstPanelId, PanelId secondPanelId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.SwapPanels(firstPanelId, secondPanelId);
    }

    public void ClosePanel(WorkspaceId workspaceId, PanelId panelToClose)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        workspaceViewModel.ClosePanel(panelToClose);
    }
}
