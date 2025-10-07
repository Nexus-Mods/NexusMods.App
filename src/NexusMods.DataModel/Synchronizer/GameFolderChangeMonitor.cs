using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.IO;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Synchronizer;

/// <summary>
/// Watches game folders for changes and emits an event once changes settle.
/// </summary>
public class GameFolderChangeMonitor : IGameFolderChangeMonitor, IDisposable
{
    private readonly ILogger<GameFolderChangeMonitor> _logger;
    private readonly IConnection _conn;
    private readonly IGameRegistry _gameRegistry;

    private readonly TimeSpan _debounce = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<EntityId, List<FileSystemWatcher>> _watchers = new();
    private readonly ConcurrentDictionary<EntityId, System.Threading.Timer> _timers = new();

    private readonly IObservable<GameInstallation> _changesSettled;
    private readonly IObserver<GameInstallation> _changesSettledObserver;

    private volatile bool _started = false;

    public GameFolderChangeMonitor(ILogger<GameFolderChangeMonitor> logger, IConnection conn, IGameRegistry gameRegistry)
    {
        _logger = logger;
        _conn = conn;
        _gameRegistry = gameRegistry;

        var subject = new System.Reactive.Subjects.Subject<GameInstallation>();
        _changesSettledObserver = subject;
        _changesSettled = subject.AsObservable();
    }

    public IObservable<GameInstallation> ChangesSettled => _changesSettled;

    public void Start()
    {
        if (_started) return;
        _started = true;

        try
        {
            // Determine which installations have any loadouts
            var db = _conn.Db;
            var loadouts = Loadout.All(db);
            var installationsToWatch = new HashSet<EntityId>(loadouts.Select(l => Loadout.Installation.Get(l)));

            foreach (var (installId, installation) in _gameRegistry.Installations)
            {
                if (!installationsToWatch.Contains(installId))
                    continue;

                SetupWatchersFor(installation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start GameFolderChangeMonitor");
        }
    }

    private void SetupWatchersFor(GameInstallation installation)
    {
        try
        {
            var list = new List<FileSystemWatcher>();
            var installId = installation.GameMetadataId;

            foreach (var (_, rootPath) in installation.LocationsRegister.GetTopLevelLocations())
            {
                if (!rootPath.DirectoryExists()) continue;

                var watcher = new FileSystemWatcher
                {
                    Path = rootPath.ToString(),
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                    Filter = "*.*",
                    EnableRaisingEvents = true,
                };

                watcher.Created += (_, __) => OnFsEvent(installId);
                watcher.Changed += (_, __) => OnFsEvent(installId);
                watcher.Deleted += (_, __) => OnFsEvent(installId);
                watcher.Renamed += (_, __) => OnFsEvent(installId);
                watcher.Error += (_, args) => _logger.LogWarning(args.GetException(), "FileSystemWatcher error for installation {InstallId}", installId);

                list.Add(watcher);
            }

            _watchers[installId] = list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to setup watchers for installation {Install}", installation.GameMetadataId);
        }
    }

    private void OnFsEvent(EntityId installationId)
    {
        try
        {
            _logger.LogTrace("FS event for installation {InstallId}", installationId);
            // Reset timer for this installation
            var timer = _timers.AddOrUpdate(
                installationId,
                key => NewTimer(key),
                (key, existing) => { existing.Change(_debounce, Timeout.InfiniteTimeSpan); return existing; }
            );
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed handling FS event for installation {InstallId}", installationId);
        }
    }

    private System.Threading.Timer NewTimer(EntityId installationId)
    {
        return new System.Threading.Timer(_ =>
        {
            try
            {
                if (!_gameRegistry.Installations.TryGetValue(installationId, out var installation))
                    return;

                _logger.LogDebug("FS changes settled for installation {InstallId}", installationId);
                _changesSettledObserver.OnNext(installation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in debounce callback for installation {InstallId}", installationId);
            }
        }, null, _debounce, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        foreach (var (_, list) in _watchers)
        {
            foreach (var w in list)
            {
                try { w.EnableRaisingEvents = false; w.Dispose(); } catch { /* ignore */ }
            }
        }
        _watchers.Clear();

        foreach (var (_, t) in _timers)
        {
            try { t.Dispose(); } catch { /* ignore */ }
        }
        _timers.Clear();
    }
}
