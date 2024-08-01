using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using ReactiveUI;

namespace NexusMods.DataModel.Synchronizer;

/// <inheritdoc />
public class SynchronizerService : ISynchronizerService
{
    private readonly ILogger<SynchronizerService> _logger;
    private readonly IConnection _conn;
    private readonly IGameRegistry _gameRegistry;
    private readonly Dictionary<EntityId, SynchronizerState> _gameStates;
    private readonly Dictionary<LoadoutId, SynchronizerState> _loadoutStates;
    private readonly object _lock = new();

    /// <summary>
    /// DI Constructor
    /// </summary>
    public SynchronizerService(IConnection conn, ILogger<SynchronizerService> logger, IGameRegistry gameRegistry)
    {
        _logger = logger;
        _conn = conn;
        _gameRegistry = gameRegistry;
        _gameStates = _gameRegistry.Installations.ToDictionary(e => e.Key, _ => new SynchronizerState());
        _loadoutStates = Loadout.All(conn.Db).ToDictionary(e => e.LoadoutId, _ => new SynchronizerState());
    }
    
    /// <inheritdoc />
    public FileDiffTree GetApplyDiffTree(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(_conn.Db, loadoutId);
        _logger.LogDebug("Getting diff tree for loadout {LoadoutId}", loadoutId);
        throw new NotImplementedException();
        /*
        var prevDiskState = _diskStateRegistry.GetState(loadout.InstallationInstance)!;
            
        var syncrhonizer = loadout.InstallationInstance.GetGame().Synchronizer;
        
        _logger.LogDebug("Creating diff tree for loadout {LoadoutId}", loadoutId);
        return syncrhonizer.LoadoutToDiskDiff(loadout, prevDiskState);
        */
    }

    /// <inheritdoc />
    public async Task Synchronize(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(_conn.Db, loadoutId);
        
        var loadoutState = GetOrAddLoadoutState(loadoutId);
        using var _ = loadoutState.WithLock();

        var gameState = GetOrAddLoadoutState(loadout.InstallationInstance.GameMetadataId);
        using var _2 = gameState.WithLock();
        
        await loadout.InstallationInstance.GetGame().Synchronizer.Synchronize(loadout);
    }

    private SynchronizerState GetOrAddLoadoutState(LoadoutId loadoutId)
    {
        lock (_lock)
        {
            if (!_loadoutStates.TryGetValue(loadoutId, out var state))
            {
                _loadoutStates[loadoutId] = state = new SynchronizerState();
            }

            return state;
        }
    }
    
    private SynchronizerState GetOrAddGameState(EntityId gameId)
    {
        lock (_lock)
        {
            if (!_gameStates.TryGetValue(gameId, out var state))
            {
                _loadoutStates[gameId] = state = new SynchronizerState();
            }
            return state;
        }
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout)
    {
        throw new NotImplementedException();
        /*
        if (!_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var lastId))
        {
            loadout = default(Loadout.ReadOnly);
            return false;
        }
        
        var db = _conn.AsOf(lastId.Tx);
        loadout = Loadout.Load(db, lastId.Id);
        return true;
        */
    }

    /// <inheritdoc />
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        throw new NotImplementedException();
        /*
    LoadoutWithTxId last;
    if (_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var lastId))
        last = lastId;
    else
        last = new LoadoutWithTxId(LoadoutId.From(EntityId.From(0)), TxId.From(0));

    // Return a deferred observable that computes the starting value only on first subscription
    return Observable.Defer(() => _diskStateRegistry.LastAppliedRevisionObservable
        .Where(x => x.Install.Equals(gameInstallation))
        .Select(x => x.LoadoutRevisionId)
        .StartWith(last)
    );
    */
    }

    /// <inheritdoc />
    public IObservable<LoadoutSynchronizerState> StatusFor(LoadoutId loadoutId)
    {
        var loadout = Loadout.Load(_conn.Db, loadoutId);
        var gameState = GetOrAddLoadoutState(loadoutId);
        var loadoutState = GetOrAddLoadoutState(loadoutId);
        
        var isBusy = Observable.CombineLatest(gameState.ObservableForProperty(g => g.Busy), 
            loadoutState.ObservableForProperty(l => l.Busy),
            (g, l) => g.Value || l.Value);
        
        var lastApplied = LastAppliedRevisionFor(loadout.InstallationInstance);
        
        var revisions = Loadout.RevisionsWithChildUpdates(_conn, loadoutId);
        
        return Observable.CombineLatest(isBusy, lastApplied, revisions, (busy, last, rev) =>
        {
            var currentDb = _conn.Db;
            if (busy)
                return LoadoutSynchronizerState.Pending;
            
            // Last DB revision is the same in the applied loadout
            if (last.Id == rev.LoadoutId && currentDb.BasisTxId == last.Tx)
                return LoadoutSynchronizerState.Current;
            
            if (last.Id != loadoutId)
                return LoadoutSynchronizerState.OtherLoadoutSynced;
            
            _logger.LogInformation("Checking for changes in loadout {LoadoutId}", loadoutId);
            var diffTree = GetApplyDiffTree(loadoutId);
            var diffFound = diffTree.GetAllDescendentFiles().Any(f => f.Item.Value.ChangeType != FileChangeType.None);
            _logger.LogInformation("Changes found in loadout {LoadoutId}: {DiffFound}", loadoutId, diffFound);
            if (diffFound)
                return LoadoutSynchronizerState.NeedsSync;
            

            return LoadoutSynchronizerState.Current;
        });
            
    }
}
