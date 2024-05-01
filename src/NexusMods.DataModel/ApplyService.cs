using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Loadouts.Extensions;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

/// <inheritdoc />
public class ApplyService : IApplyService
{
    private readonly ILogger<ApplyService> _logger;
    private readonly IDiskStateRegistry _diskStateRegistry;
    private readonly IConnection _conn;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ApplyService(IDiskStateRegistry diskStateRegistry, IConnection conn, ILogger<ApplyService> logger)
    {
        _logger = logger;
        _conn = conn;
        _diskStateRegistry = diskStateRegistry;
    }

    /// <inheritdoc />
    public async Task<Loadout.Model> Apply(Loadout.Model loadout)
    {
        // TODO: Check if this or any other loadout is being applied to this game installation
        // Queue the loadout to be applied if that is the case.

        _logger.LogInformation(
            "Applying loadout {Name} to {GameName} {GameVersion}",
            loadout.Name,
            loadout.Installation.Game.Name,
            loadout.Installation.Version
        );

        try
        {
            await loadout.Apply();
        }
        catch (NeedsIngestException)
        {
            _logger.LogInformation("Ingesting loadout {Name} from {GameName} {GameVersion}", loadout.Name,
                loadout.Installation.Game.Name, loadout.Installation.Version
            );

            var lastAppliedLoadout = GetLastAppliedLoadout(loadout.Installation);
            if (lastAppliedLoadout is not null)
            {
                _logger.LogInformation("Last applied loadout found: {LoadoutId} as of {TxId}", lastAppliedLoadout.Id, lastAppliedLoadout.Db.BasisTxId);
            }
            else
            {
                // There is apparently no last applied revision, so we'll just use the loadout we're trying to apply
                lastAppliedLoadout = loadout;
            }

            var loadoutWithIngest = await loadout.Ingest();

            await loadoutWithIngest.Apply();
            return loadoutWithIngest;
        }
        
        return loadout;
    }


    /// <inheritdoc />
    public ValueTask<FileDiffTree> GetApplyDiffTree(Loadout.Model loadout)
    {
        var prevDiskState = _diskStateRegistry.GetState(loadout.Installation)!;
            
        var syncrhonizer = loadout.Installation.GetGame().Synchronizer;
        
        return syncrhonizer.LoadoutToDiskDiff(loadout, prevDiskState);
    }

    /// <inheritdoc />
    public async Task<Loadout.Model> Ingest(GameInstallation gameInstallation)
    {
        
        var lastAppliedRevision = GetLastAppliedLoadout(gameInstallation);
        if (lastAppliedRevision is null)
        {
            throw new InvalidOperationException("Game installation does not have a last applied loadout to ingest into");
        }

        var lastLoadout = _conn.Db.Get<Loadout.Model>(lastAppliedRevision.Id);
        var lastAppliedLoadout = lastLoadout ?? throw new KeyNotFoundException("Loadout not found for last applied revision");
        
        var loadoutWithIngest = await lastAppliedLoadout.Ingest();

        return loadoutWithIngest;
    }

    /// <inheritdoc />
    public Loadout.Model? GetLastAppliedLoadout(GameInstallation gameInstallation)
    {
        if (!_diskStateRegistry.TryGetLastAppliedLoadout(gameInstallation, out var loadout, out var txId))
        {
            return null;
        }
        
        var db = _conn.AsOf(txId);
        return db.Get<Loadout.Model>(loadout);
    }

    /// <inheritdoc />
    public IObservable<IId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        throw new NotImplementedException();
        /*
        // Return a deferred observable that computes the starting value only on first subscription
        return Observable.Defer(() => _diskStateRegistry.LastAppliedRevisionObservable
            .Where(x => x.gameInstallation.Equals(gameInstallation))
            .Select(x => x.loadoutRevision)
            .StartWith(_diskStateRegistry.GetLastAppliedLoadout(gameInstallation) ?? IdEmpty.Empty)
        );
        */
    }
}
