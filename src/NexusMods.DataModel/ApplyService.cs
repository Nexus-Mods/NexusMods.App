using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.DataModel.Loadouts.Extensions;

namespace NexusMods.DataModel;

/// <inheritdoc />
public class ApplyService : IApplyService
{
    private readonly ILoadoutRegistry _loadoutRegistry;
    private readonly ILogger<ApplyService> _logger;
    private readonly IDiskStateRegistry _diskStateRegistry;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ApplyService(ILoadoutRegistry loadoutRegistry, IDiskStateRegistry diskStateRegistry, ILogger<ApplyService> logger)
    {
        _loadoutRegistry = loadoutRegistry;
        _logger = logger;
        _diskStateRegistry = diskStateRegistry;
    }

    /// <inheritdoc />
    public async Task<Loadout> Apply(LoadoutId loadoutId)
    {
        // TODO: Check if this or any other loadout is being applied to this game installation
        // Queue the loadout to be applied if that is the case.

        var loadout = _loadoutRegistry.Get(loadoutId);
        if (loadout is null)
            throw new ArgumentException("Loadout not found", nameof(loadoutId));

        _logger.LogInformation(
            "Applying loadout {LoadoutId} to {GameName} {GameVersion}",
            loadout.LoadoutId,
            loadout.Installation.Game.Name,
            loadout.Installation.Version
        );

        try
        {
            await loadout.Apply();
        }
        catch (NeedsIngestException)
        {
            _logger.LogInformation("Ingesting loadout {LoadoutId} from {GameName} {GameVersion}", loadout.LoadoutId,
                loadout.Installation.Game.Name, loadout.Installation.Version
            );

            Loadout lastAppliedLoadout;
            var lastAppliedRevision = GetLastAppliedLoadout(loadout.Installation);
            if (lastAppliedRevision is not null)
            {
                var lastLoadout = _loadoutRegistry.GetLoadout(lastAppliedRevision);
                lastAppliedLoadout = lastLoadout ?? throw new KeyNotFoundException("Loadout not found for last applied revision");
            }
            else
            {
                // There is apparently no last applied revision, so we'll just use the loadout we're trying to apply
                lastAppliedLoadout = loadout;
            }

            var loadoutWithIngest = await lastAppliedLoadout.Ingest();

            // Rebase unapplied changes on top of ingested changes
            var mergedLoadout = _loadoutRegistry.Alter(loadout.LoadoutId, $"Rebase unapplied changes on top of ingested changes in loadout: {loadout.Name}",
                l => l.Installation.GetGame().Synchronizer.MergeLoadouts(loadoutWithIngest, loadout)
            );

            _logger.LogInformation("Applying loadout {LoadoutId} to {GameName} {GameVersion}", mergedLoadout.LoadoutId,
                mergedLoadout.Installation.Game.Name, mergedLoadout.Installation.Version
            );

            await mergedLoadout.Apply();
            return mergedLoadout;
        }

        return loadout;
    }

    /// <inheritdoc />
    public async Task<Loadout> Ingest(GameInstallation gameInstallation)
    {
        var lastAppliedRevision = GetLastAppliedLoadout(gameInstallation);
        if (lastAppliedRevision is null)
        {
            throw new InvalidOperationException("Game installation does not have a last applied loadout to ingest into");
        }

        var lastLoadout = _loadoutRegistry.GetLoadout(lastAppliedRevision);
        var lastAppliedLoadout = lastLoadout ?? throw new KeyNotFoundException("Loadout not found for last applied revision");
        
        var loadoutWithIngest = await lastAppliedLoadout.Ingest();
        
        // Get the latest loadout revision
        var latestRevision = _loadoutRegistry.Get(loadoutWithIngest.LoadoutId);
        if (latestRevision is null)
            throw new KeyNotFoundException("No latest revision found for last applied loadout");
        
        // if latest revision is the same as the last applied revision, no need to rebase
        if (latestRevision.DataStoreId.Equals(loadoutWithIngest.DataStoreId))
        {
            var newLoadout = _loadoutRegistry.Alter(loadoutWithIngest.LoadoutId, $"Ingested changes in loadout: {loadoutWithIngest.Name}",
                l => loadoutWithIngest
            );
            return newLoadout;
        }
        
        // Rebase unapplied changes on top of ingested changes
        var mergedLoadout = _loadoutRegistry.Alter(latestRevision.LoadoutId, $"Rebase unapplied changes on top of ingested changes in loadout: {latestRevision.Name}",
            l => l.Installation.GetGame().Synchronizer.MergeLoadouts(loadoutWithIngest, latestRevision)
        );
        
        return mergedLoadout;
    }

    /// <inheritdoc />
    public IId? GetLastAppliedLoadout(GameInstallation gameInstallation)
    {
        var loadoutRevision = _diskStateRegistry.GetLastAppliedLoadout(gameInstallation);
        return loadoutRevision;
    }

    /// <inheritdoc />
    public IObservable<IId> LastAppliedRevisionFor(GameInstallation gameInstallation)
    {
        // Return a deferred observable that computes the starting value only on first subscription
        return Observable.Defer(() => _diskStateRegistry.LastAppliedRevisionObservable
            .Where(x => x.gameInstallation.Equals(gameInstallation))
            .Select(x => x.loadoutRevision)
            .StartWith(_diskStateRegistry.GetLastAppliedLoadout(gameInstallation) ?? IdEmpty.Empty)
        );
    }
}
