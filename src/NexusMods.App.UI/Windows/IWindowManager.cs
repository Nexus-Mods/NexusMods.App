using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace NexusMods.App.UI.Windows;

/// <summary>
/// Represents a manager of all existing windows.
/// </summary>
[PublicAPI]
public interface IWindowManager
{
    /// <summary>
    /// Gets the ID of the currently active window.
    /// </summary>
    public WindowId ActiveWindowId { get; set; }

    /// <summary>
    /// Gets a read-only observable collection containing the IDs of all existing windows.
    /// </summary>
    public ReadOnlyObservableCollection<WindowId> AllWindowIds { get; }

    /// <summary>
    /// Tries to get the currently active window.
    /// </summary>
    public bool TryGetActiveWindow([NotNullWhen(true)] out IWorkspaceWindow? window);

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
}
