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
        var synchronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        var metaData = GameInstallMetadata.Load(_conn.Db, loadout.InstallationInstance.GameMetadataId);
        var diskState = metaData.DiskStateAsOf(metaData.LastScannedDiskStateTransaction);
        
        return synchronizer.LoadoutToDiskDiff(loadout, diskState);
        
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
        var metadata = gameInstallation.GetMetadata(_conn);
        
        if (GameInstallMetadata.LastSyncedLoadout.TryGet(metadata, out var lastId))
        {
            loadout = Loadout.Load(_conn.Db, lastId);
            return true;
        }
        
        loadout = default(Loadout.ReadOnly);
        return false;
    }

    /// <inheritdoc />
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        return GameInstallMetadata.Observe(_conn, gameInstallation.GameMetadataId)
            .Select(metadata =>
                {
                    if (GameInstallMetadata.LastSyncedLoadout.TryGet(metadata, out var lastId) && GameInstallMetadata.LastSyncedLoadoutTransaction.TryGet(metadata, out var txId))
                    {
                        return new LoadoutWithTxId(lastId, TxId.From(txId.Value));
                    }

                    return default(LoadoutWithTxId);
                }
            );
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
