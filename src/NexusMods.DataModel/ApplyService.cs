using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
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
    private readonly IObservable<(GameInstallation gameInstallation, IId loadoutRevision)> _lastAppliedRevisionObservable;

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ApplyService(ILoadoutRegistry loadoutRegistry, IDiskStateRegistry diskStateRegistry, ILogger<ApplyService> logger)
    {
        _loadoutRegistry = loadoutRegistry;
        _logger = logger;
        _diskStateRegistry = diskStateRegistry;

        _lastAppliedRevisionObservable = _diskStateRegistry.LastAppliedRevisionObservable;
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
                lastAppliedLoadout = lastLoadout ?? throw new Exception("Loadout not found for last applied revision");
            }
            else
            {
                // There is apparently no last applied revision, so we'll just use the loadout we're trying to apply
                lastAppliedLoadout = loadout;
            }

            // TODO: Actually do something with the loadoutWithIngest, right now we just ignore it
            var loadoutWithIngest = await lastAppliedLoadout.Ingest();

            _logger.LogInformation("Applying loadout {LoadoutId} to {GameName} {GameVersion}", loadout.LoadoutId,
                loadout.Installation.Game.Name, loadout.Installation.Version
            );
            
            // TODO: Apply a loadout containing both the ingest and the unapplied changes
            await loadout.Apply();
        }

        return loadout;
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
        return Observable.Defer(() => _lastAppliedRevisionObservable
            .Where(x => x.gameInstallation.Equals(gameInstallation))
            .Select(x => x.loadoutRevision)
            .StartWith(_diskStateRegistry.GetLastAppliedLoadout(gameInstallation) ?? IdEmpty.Empty)
        );
    }
}
