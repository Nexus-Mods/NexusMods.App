using System.Collections.ObjectModel;
using Avalonia.Media;
using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.App.UI.Windows;

namespace NexusMods.App.UI.WorkspaceSystem;

/// <summary>
/// Represents a controller for all workspaces inside a window.
/// </summary>
[PublicAPI]
public interface IWorkspaceController
{
    /// <summary>
    /// Gets the ID of the window that is associated with this controller.
    /// </summary>
    public WindowId WindowId { get; }

    /// <summary>
    /// Gets a read-only observable collection of all workspaces.
    /// </summary>
    public ReadOnlyObservableCollection<IWorkspaceViewModel> AllWorkspaces { get; }

    /// <summary>
    /// Creates a new workspace with one panel and a tab.
    /// </summary>
    /// <param name="context">Optional <see cref="IWorkspaceContext"/> for the workspace. If this is <see cref="Optional{T}.None"/> the <see cref="EmptyContext"/> will be used.</param>
    /// <param name="pageData">Optional <see cref="PageData"/> for the first tab. If this is <see cref="Optional{T}.None"/> the default tab will be shown.</param>
    public IWorkspaceViewModel CreateWorkspace(Optional<IWorkspaceContext> context, Optional<PageData> pageData);

    /// <summary>
    /// Changes the active workspace of the window.
    /// </summary>
    public void ChangeActiveWorkspace(WorkspaceId workspaceId);

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
