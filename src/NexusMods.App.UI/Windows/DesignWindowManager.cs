using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Windows;

public class DesignWindowManager : IWindowManager
{
    public static readonly IWindowManager Instance = new DesignWindowManager();

    public WindowId ActiveWindowId { get; set; } = WindowId.DefaultValue;

    public ReadOnlyObservableCollection<WindowId> AllWindowIds { get; } = ReadOnlyObservableCollection<WindowId>.Empty;

    public bool TryGetActiveWindow([NotNullWhen(true)] out IWorkspaceWindow? window)
    {
        window = null;
        return false;
    }

    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window)
    {
        window = null;
        return false;
    }

    public void RegisterWindow(IWorkspaceWindow window) { }

    public void UnregisterWindow(IWorkspaceWindow window) { }

    public void SaveWindowState(IWorkspaceWindow window) { }
    public bool RestoreWindowState(IWorkspaceWindow window) => false;
}
