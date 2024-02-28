using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// DI Constructor
    /// </summary>
    public ApplyService(ILoadoutRegistry loadoutRegistry, ILogger<ApplyService> logger)
    {
        _loadoutRegistry = loadoutRegistry;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Loadout> Apply(LoadoutId loadoutId)
    {
        // TODO: Check if this or any other loadout is being applied to this game installation
        // Queue the loadout to be applied if that is the case.

        var loadout = _loadoutRegistry.Get(loadoutId);
        if (loadout is null)
            throw new ArgumentException("Loadout not found", nameof(loadoutId));

        _logger.LogInformation("Applying loadout {LoadoutId} to {GameName} {GameVersion}", loadout.LoadoutId,
            loadout.Installation.Game.Name, loadout.Installation.Version);
        try
        {
            await loadout.Apply();
        }
        catch (NeedsIngestException)
        {
            _logger.LogInformation("Ingesting loadout {LoadoutId} from {GameName} {GameVersion}", loadout.LoadoutId,
                loadout.Installation.Game.Name, loadout.Installation.Version);
            await loadout.Ingest();
            _logger.LogInformation("Applying loadout {LoadoutId} to {GameName} {GameVersion}", loadout.LoadoutId,
                loadout.Installation.Game.Name, loadout.Installation.Version);
            await loadout.Apply();
        }

        return loadout;
    }

    /// <inheritdoc />
    public (LoadoutId, IId) GetLastAppliedLoadout(GameInstallation gameInstallation)
    {
        // TODO: Keep track of the last applied loadout and revision for each game installation
        throw new NotImplementedException();
    }
}
