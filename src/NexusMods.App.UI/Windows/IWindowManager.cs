using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NexusMods.App.UI.MessageBox.Enums;
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

    public Task<ButtonResult> ShowModalAsync(string title, string text, ButtonEnum buttonEnum);
    
    public Task<ButtonResult> ShowModelessAsync(string title, string text, ButtonEnum buttonEnum);
    
    public Task<ButtonResult> ShowEmbeddedAsync(string title, string text);
}
