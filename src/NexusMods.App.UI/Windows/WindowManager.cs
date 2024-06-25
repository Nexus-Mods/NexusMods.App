using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Windows;

internal sealed class WindowManager : ReactiveObject, IWindowManager
{
    private readonly ILogger<WindowManager> _logger;
    private readonly IConnection _conn;

    private readonly Dictionary<WindowId, WeakReference<IWorkspaceWindow>> _windows = new();
    private readonly SourceList<WindowId> _allWindowIdSource = new();

    public WindowManager(
        ILogger<WindowManager> logger,
        IConnection conn)
    {
        _logger = logger;
        _conn = conn;
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

            using var tx = _conn.BeginTransaction();
            var found = WindowDataAttributes.All(_conn.Db).FirstOrDefault();
            if (!found.IsValid())
            {
                var model = new WindowDataAttributes.New(tx)
                {
                    Data = WindowDataAttributes.Encode(_conn.Db, data),
                };
            }
            else
            {
                tx.Add(found.Id, WindowDataAttributes.Data, WindowDataAttributes.Encode(_conn.Db, data));
            }
            tx.Commit();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while saving window state");
        }
    }

    public bool RestoreWindowState(IWorkspaceWindow window)
    {
        try
        {
            if (!WindowDataAttributes.All(_conn.Db).TryGetFirst(out var found))
                return false;

            window.WorkspaceController.FromData(found.WindowData);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while loading window state");

            _logger.LogInformation("Removing possible broken window state from the DataStore");

            try
            {
                using var tx = _conn.BeginTransaction();
                var found = WindowDataAttributes.All(_conn.Db).First();
                if (!found.IsValid())
                    return false;
                tx.Delete(found.Id, true);
                tx.Commit();
            }
            catch (Exception otherException)
            {
                _logger.LogError(otherException, "Exception while retracting window state");
            }
        }

        return false;
    }
}
