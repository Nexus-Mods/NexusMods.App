using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using ReactiveUI;

namespace NexusMods.App.UI.Windows;

internal sealed class WindowManager : ReactiveObject, IWindowManager
{
    private readonly ILogger<WindowManager> _logger;
    private readonly IConnection _connection;

    private readonly Dictionary<WindowId, WeakReference<IWorkspaceWindow>> _windows = new();
    private readonly SourceList<WindowId> _allWindowIdSource = new();

    public WindowManager(
        ILogger<WindowManager> logger,
        IConnection connection)
    {
        _logger = logger;
        _connection = connection;
        _allWindowIdSource.Connect().OnUI().Bind(out _allWindowIds);
    }

    private WindowId _activeWindowId = WindowId.DefaultValue;
    public IWorkspaceWindow ActiveWindow {
        get => GetActiveWindow();
        set => SetActiveWindow(value);
    }

    private readonly ReadOnlyObservableCollection<WindowId> _allWindowIds;
    public ReadOnlyObservableCollection<WindowId> AllWindowIds => _allWindowIds;

    private IWorkspaceWindow GetActiveWindow()
    {
        if (TryGetWindow(_activeWindowId, out var window)) return window;
        throw new InvalidOperationException("There is no active window");
    }

    private void SetActiveWindow(IWorkspaceWindow window)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (!TryGetWindow(window.WindowId, out _))
            throw new InvalidOperationException($"Can't change active window to unregistered window `{window.WindowId}`");

        _activeWindowId = window.WindowId;
        this.RaisePropertyChanged(nameof(ActiveWindow));
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
        SetActiveWindow(window);
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

            using var tx = _connection.BeginTransaction();
            var found = WindowDataAttributes.All(_connection.Db).FirstOrDefault();
            if (!found.IsValid())
            {
                _ = new WindowDataAttributes.New(tx)
                {
                    Data = WindowDataAttributes.Encode(_connection.Db, data),
                };
            }
            else
            {
                tx.Add(found.Id, WindowDataAttributes.Data, WindowDataAttributes.Encode(_connection.Db, data));
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
        Optional<WindowDataAttributes.ReadOnly> optionalWindowData;

        try
        {
            optionalWindowData = WindowDataAttributes.All(_connection.Db).FirstOrOptional(static data => data.IsValid());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception loading saved window state");
            ResetSavedData();
            return false;
        }

        if (!optionalWindowData.HasValue)
        {
            ResetSavedData();
            return false;
        }

        try
        {
            window.WorkspaceController.FromData(optionalWindowData.Value.WindowData);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception restoring saved window state");
            ResetSavedData();
            return false;
        }
    }

    private void ResetSavedData()
    {
        _logger.LogInformation("Removing possible broken window state from the DB");
        using var tx = _connection.BeginTransaction();

        foreach (var datom in _connection.Db.Datoms(WindowDataAttributes.PrimaryAttribute))
        {
            tx.Delete(datom.E, recursive: true);
        }

        tx.Commit();
    }
}
