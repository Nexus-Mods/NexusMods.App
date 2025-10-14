using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

[PublicAPI]
public interface ILoadoutManager
{
    /// <summary>
    /// Creates a loadout for a game, managing the game if it has not previously been managed.
    /// </summary>
    IJobTask<CreateLoadoutJob, Loadout.ReadOnly> CreateLoadout(GameInstallation installation, string? suggestedName = null);

    /// <summary>
    /// Copies a loadout.
    /// </summary>
    ValueTask<Loadout.ReadOnly> CopyLoadout(LoadoutId loadoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the loadout for the game. If the loadout is the currently active loadout,
    /// the game's folder will be reset to its initial state.
    /// </summary>
    ValueTask DeleteLoadout(LoadoutId loadoutId, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.DoNotRun, bool deactivateIfActive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the loadout as the active loadout for the game, applying the changes to the game folder.
    /// </summary>
    ValueTask ActivateLoadout(LoadoutId loadoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a game back to its initial state, any applied loadouts will be unapplied.
    /// </summary>
    ValueTask DeactivateCurrentLoadout(GameInstallation installation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active loadout for the game, if any.
    /// </summary>
    Optional<LoadoutId> GetCurrentlyActiveLoadout(GameInstallation installation);

    /// <summary>
    /// Removes all the loadouts for a game, and resets the game folder to its initial state.
    /// </summary>
    IJobTask<UnmanageGameJob, GameInstallation> UnManage(GameInstallation installation, bool runGc = true, bool cleanGameFolder = true, CancellationToken cancellationToken = default);
}
