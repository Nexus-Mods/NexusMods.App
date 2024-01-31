using System.Diagnostics.CodeAnalysis;

namespace NexusMods.App.UI.Windows;

public interface IWindowManager
{
    public WindowId ActiveWindowId { get; set; }

    public bool TryGetActiveWindow([NotNullWhen(true)] out IWorkspaceWindow? window);

    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window);

    public void RegisterWindow(IWorkspaceWindow window);

    public void UnregisterWindow(IWorkspaceWindow window);
}
