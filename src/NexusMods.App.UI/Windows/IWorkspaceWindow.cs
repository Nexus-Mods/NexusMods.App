using System.Reactive;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

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
    
    /// <summary>
    ///     This command is used to bring the window to front.
    /// </summary>
    ReactiveCommand<Unit, bool> BringWindowToFront { get; }
}
