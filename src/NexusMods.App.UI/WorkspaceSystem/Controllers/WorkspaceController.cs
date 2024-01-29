using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
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

    private bool TryGetPanel(IWorkspaceViewModel workspaceViewModel, PanelId panelId, [NotNullWhen(true)] out IPanelViewModel? panelViewModel)
    {
        panelViewModel = workspaceViewModel.Panels.FirstOrDefault(panel => panel.Id == panelId);
        if (panelViewModel is null)
        {
            _logger.LogError("Failed to find Panel with ID {PanelID} in Workspace with ID {WorkspaceID}", panelId, workspaceViewModel.Id);
            return false;
        }

        return true;
    }

    private bool TryGetTab(IPanelViewModel panelViewModel, PanelTabId tabId, [NotNullWhen(true)] out IPanelTabViewModel? tabViewModel)
    {
        tabViewModel = panelViewModel.Tabs.FirstOrDefault(tab => tab.Id == tabId);
        if (tabViewModel is null)
        {
            _logger.LogError("Failed to find Tab with ID {TabID} in Panel with ID {PanelID} in Workspace with ID {WorkspaceID}", tabId, panelViewModel.Id, panelViewModel.WorkspaceId);
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

    public void SetTabTitle(string title, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Title = title;
    }

    public void SetIcon(IImage? icon, WorkspaceId workspaceId, PanelId panelId, PanelTabId tabId)
    {
        if (!TryGetWorkspace(workspaceId, out var workspaceViewModel)) return;
        if (!TryGetPanel(workspaceViewModel, panelId, out var panelViewModel)) return;
        if (!TryGetTab(panelViewModel, tabId, out var tabViewModel)) return;

        tabViewModel.Header.Icon = icon;
    }
}
