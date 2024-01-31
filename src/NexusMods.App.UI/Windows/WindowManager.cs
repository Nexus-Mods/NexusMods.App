using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

internal class WindowManager : IWindowManager
{
    private readonly ILogger<WindowManager> _logger;
    private readonly Dictionary<WindowId, WeakReference<IWorkspaceWindow>> _windows = new();

    public WindowManager(ILogger<WindowManager> logger)
    {
        _logger = logger;
    }

    [Reactive] public WindowId ActiveWindowId { get; set; } = WindowId.DefaultValue;

    public bool TryGetActiveWindow([NotNullWhen(true )] out IWorkspaceWindow? window)
    {
        return TryGetWindow(ActiveWindowId, out window);
    }

    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window)
    {
        window = null;

        if (!_windows.TryGetValue(ActiveWindowId, out var weakReference))
        {
            _logger.LogError("Failed to find Window with ID {WindowId}", windowId);
            return false;
        }

        if (!weakReference.TryGetTarget(out window))
        {
            _logger.LogError("Failed to retrieve Window with the ID {WorkspaceID} referenced by the WeakReference", windowId);
            return false;
        }

        return true;
    }

    public void RegisterWindow(IWorkspaceWindow window)
    {
        if (!_windows.TryAdd(window.WindowId, new WeakReference<IWorkspaceWindow>(window)))
        {
            _logger.LogError("Unable to register Window with ID {WindowId}", window.WindowId);
        }
    }

    public void UnregisterWindow(IWorkspaceWindow window)
    {
        if (!_windows.Remove(window.WindowId))
        {
            _logger.LogError("Unable to unregister Window with ID {WindowId}", window.WindowId);
        }
    }
}
