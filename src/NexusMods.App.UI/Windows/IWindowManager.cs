using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.App.UI.Dialog;
using NexusMods.App.UI.Dialog.Enums;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Windows;

/// <summary>
/// Represents a manager of all existing windows.
/// </summary>
[PublicAPI]
public interface IWindowManager
{
    /// <summary>
    /// Gets or sets the currently active window.
    /// </summary>
    public IWorkspaceWindow ActiveWindow { get; set; }

    /// <summary>
    /// Gets the workspace controller of the currently active window.
    /// </summary>
    public IWorkspaceController ActiveWorkspaceController => ActiveWindow.WorkspaceController;

    /// <summary>
    /// Gets a read-only observable collection containing the IDs of all existing windows.
    /// </summary>
    public ReadOnlyObservableCollection<WindowId> AllWindowIds { get; }

    /// <summary>
    /// Tries to get a window.
    /// </summary>
    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window);

    /// <summary>
    /// Registers a new window with the manager.
    /// </summary>
    public void RegisterWindow(IWorkspaceWindow window);

    /// <summary>
    /// Unregisters a window with the manager.
    /// </summary>
    /// <remarks>
    /// This should be called when the window gets disposed.
    /// </remarks>
    public void UnregisterWindow(IWorkspaceWindow window);

    /// <summary>
    /// Saves current state of the window.
    /// </summary>
    /// <param name="window"></param>
    public void SaveWindowState(IWorkspaceWindow window);

    /// <summary>
    /// Restores the saved window state.
    /// </summary>
    /// <param name="window"></param>
    /// <returns>Whether the restore was successful.</returns>
    public bool RestoreWindowState(IWorkspaceWindow window);

    /// <summary>
    /// Displays a dialog window of the specified type.
    /// </summary>
    /// <param name="dialog">The dialog to display.</param>
    /// <param name="windowType">The type of the dialog window.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the button definition ID 
    /// of the button that was clicked to close the dialog.
    /// </returns>
    public Task<TResult> ShowDialog<TResult>(IDialog<TResult> dialog, DialogWindowType windowType);
}
