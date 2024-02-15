using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

internal sealed class WindowManager : ReactiveObject, IWindowManager
{
    private readonly ILogger<WindowManager> _logger;
    private readonly IDataStore _dataStore;

    private readonly Dictionary<WindowId, WeakReference<IWorkspaceWindow>> _windows = new();
    private readonly SourceList<WindowId> _allWindowIdSource = new();

    public WindowManager(
        ILogger<WindowManager> logger,
        IDataStore dataStore)
    {
        _logger = logger;
        _dataStore = dataStore;

        _allWindowIdSource.Connect().OnUI().Bind(out _allWindowIds);
    }

    [Reactive] public WindowId ActiveWindowId { get; set; } = WindowId.DefaultValue;

    private readonly ReadOnlyObservableCollection<WindowId> _allWindowIds;
    public ReadOnlyObservableCollection<WindowId> AllWindowIds => _allWindowIds;

    public bool TryGetActiveWindow([NotNullWhen(true )] out IWorkspaceWindow? window)
    {
        return TryGetWindow(ActiveWindowId, out window);
    }

    public bool TryGetWindow(WindowId windowId, [NotNullWhen(true)] out IWorkspaceWindow? window)
    {
        window = null;

        if (!_windows.TryGetValue(windowId, out var weakReference))
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
        Dispatcher.UIThread.VerifyAccess();

        if (!_windows.TryAdd(window.WindowId, new WeakReference<IWorkspaceWindow>(window)))
        {
            _logger.LogError("Unable to register Window with ID {WindowId}", window.WindowId);
            return;
        }

        _allWindowIdSource.Edit(list => list.Add(window.WindowId));
        ActiveWindowId = window.WindowId;
    }

    public void UnregisterWindow(IWorkspaceWindow window)
    {
        if (!_windows.Remove(window.WindowId))
        {
            _logger.LogError("Unable to unregister Window with ID {WindowId}", window.WindowId);
        }

        _allWindowIdSource.Edit(list => list.Remove(window.WindowId));
    }

    public void SaveWindowState(IWorkspaceWindow window)
    {
        try
        {
            var data = window.WorkspaceController.ToData();
            _dataStore.Put(WindowData.Id, data);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while saving window state");
        }
    }

    public bool RestoreWindowState(IWorkspaceWindow window)
    {
        var isFirstWindow = _windows.Count == 1;
        if (!isFirstWindow) return false;

        try
        {
            var data = _dataStore.Get<WindowData>(WindowData.Id);
            if (data is null) return false;

            window.WorkspaceController.FromData(data);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading window state");

            _logger.LogInformation("Removing possible broken window state from the DataStore");

            try
            {
                _dataStore.Delete(WindowData.Id);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return false;
    }
}
