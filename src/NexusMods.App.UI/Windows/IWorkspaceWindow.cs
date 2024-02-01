using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Windows;

/// <summary>
/// Represents a window with a workspace.
/// </summary>
public interface IWorkspaceWindow
{
    /// <summary>
    /// Gets the ID of the window.
    /// </summary>
    public WindowId WindowId { get; }

    /// <summary>
    /// Gets whether the window is active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Gets the workspace controller of the window.
    /// </summary>
    public IWorkspaceController WorkspaceController { get; }
}
