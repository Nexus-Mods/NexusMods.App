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
    private readonly TimeProvider _timeProvider;

    private readonly TimeSpan _debounce = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<EntityId, List<FileSystemWatcher>> _watchers = new();
    // Preallocated slot mapping and due times (UTC ticks); 0 means no pending debounce
    private Dictionary<EntityId, int> _slotIndexById = new();
    private EntityId[] _idsByIndex = [];
    private long[] _dueTicks = [];
    // Periodic scanner timer (fixed period, no rescheduling, no locks)
    private ITimer? _scanner;
    private readonly TimeSpan _scanInterval = TimeSpan.FromMilliseconds(500);

    private readonly IObservable<GameInstallation> _changesSettled;
    private readonly IObserver<GameInstallation> _changesSettledObserver;

    private volatile bool _started = false;

    public GameFolderChangeMonitor(ILogger<GameFolderChangeMonitor> logger, IConnection conn, IGameRegistry gameRegistry)
        : this(logger, conn, gameRegistry, TimeProvider.System)
    {
    }

    public GameFolderChangeMonitor(ILogger<GameFolderChangeMonitor> logger, IConnection conn, IGameRegistry gameRegistry, TimeProvider timeProvider)
    {
        _logger = logger;
        _conn = conn;
        _gameRegistry = gameRegistry;
        _timeProvider = timeProvider;

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

            var idsList = new List<EntityId>();
            foreach (var (installId, installation) in _gameRegistry.Installations)
            {
                if (!installationsToWatch.Contains(installId))
                    continue;

                idsList.Add(installId);
                SetupWatchersFor(installation);
            }

            // Preallocate slots for debounce tracking
            _idsByIndex = idsList.ToArray();
            _dueTicks = new long[_idsByIndex.Length];
            _slotIndexById = new Dictionary<EntityId, int>(_idsByIndex.Length);
            for (var i = 0; i < _idsByIndex.Length; i++)
                _slotIndexById[_idsByIndex[i]] = i;

            // Start periodic scanner
            _scanner = _timeProvider.CreateTimer(ScanTick, state: this, dueTime: _scanInterval, period: _scanInterval);
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
            // Record/extend due time for this installation
            if (_slotIndexById.TryGetValue(installationId, out var index) && (uint)index < (uint)_dueTicks.Length)
            {
                var nowTicks = _timeProvider.GetUtcNow().UtcTicks;
                var dueTicks = nowTicks + _debounce.Ticks;
                Volatile.Write(ref _dueTicks[index], dueTicks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed handling FS event for installation {InstallId}", installationId);
        }
    }

    private static void ScanTick(object? state)
    {
        if (state is not GameFolderChangeMonitor self) return;

        try
        {
            var nowTicks = self._timeProvider.GetUtcNow().UtcTicks;
            var ids = self._idsByIndex;
            var due = self._dueTicks;

            for (var i = 0; i < due.Length; i++)
            {
                var d = Volatile.Read(ref due[i]);
                if (d == 0) continue;
                if (nowTicks < d) continue;

                // Reset before emitting to avoid duplicate work if emission is slow
                Volatile.Write(ref due[i], 0);

                var id = ids[i];
                if (!self._gameRegistry.Installations.TryGetValue(id, out var installation))
                    continue;

                self._logger.LogDebug("FS changes settled for installation {InstallId}", id);
                self._changesSettledObserver.OnNext(installation);
            }
        }
        catch (Exception ex)
        {
            self._logger.LogWarning(ex, "Error in debounce scanner");
        }
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

        try { _scanner?.Dispose(); } catch { /* ignore */ }
        _scanner = null;
        _slotIndexById.Clear();
        _idsByIndex = [];
        _dueTicks = [];
    }
}
