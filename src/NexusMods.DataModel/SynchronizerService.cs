using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <inheritdoc />
public class SynchronizerService : ISynchronizerService
{
    private readonly ILogger<SynchronizerService> _logger;
    private readonly IDiskStateRegistry _diskStateRegistry;
    private readonly IConnection _conn;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public SynchronizerService(IDiskStateRegistry diskStateRegistry, IConnection conn, ILogger<SynchronizerService> logger)
    {
        _logger = logger;
        _conn = conn;
        _diskStateRegistry = diskStateRegistry;
    }
    
    /// <inheritdoc />
    public async Task Synchronize(Loadout.ReadOnly loadout)
    {
        await loadout.InstallationInstance.GetGame().Synchronizer.Synchronize(loadout);
    }


    /// <inheritdoc />
    public FileDiffTree GetApplyDiffTree(Loadout.ReadOnly loadout)
    {
        var prevDiskState = _diskStateRegistry.GetState(loadout.InstallationInstance)!;
            
        var syncrhonizer = loadout.InstallationInstance.GetGame().Synchronizer;
        
        return syncrhonizer.LoadoutToDiskDiff(loadout, prevDiskState);
    }

    /// <inheritdoc />
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout)
    {
        if (!_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var lastId))
        {
            loadout = default(Loadout.ReadOnly);
            return false;
        }
        
        var db = _conn.AsOf(lastId.Tx);
        loadout = Loadout.Load(db, lastId.Id);
        return true;
    }

    /// <inheritdoc />
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
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
    }
}
