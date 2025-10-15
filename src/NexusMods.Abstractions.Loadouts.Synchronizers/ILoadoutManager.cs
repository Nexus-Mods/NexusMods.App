using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.MnemonicDB.Abstractions;
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

    /// <summary>
    /// Installs a library item into a target loadout.
    /// </summary>
    /// <param name="libraryItem">The item to install.</param>
    /// <param name="targetLoadout">The target loadout.</param>
    /// <param name="parent">If specified the installed item will be placed in this group, otherwise it will default to the user's local collection</param>
    /// <param name="installer">The Library will use this installer to install the item</param>
    /// <param name="fallbackInstaller">The installer to use if the default installer fails</param>
    /// <param name="transaction">The transaction to attach the installation to. Install is only completed when transaction is completed.</param>
    /// <remarks>
    /// Job returns a result with null <see cref="LoadoutItemGroup.ReadOnly"/> after
    /// if supplied an external transaction via <paramref name="transaction"/>,
    /// since it is the caller's responsibility to complete that transaction.
    /// </remarks>
    IJobTask<IInstallLoadoutItemJob, InstallLoadoutItemJobResult> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null,
        ITransaction? transaction = null);

    /// <summary>
    /// Removes the items from their Loadouts.
    /// </summary>
    void RemoveItems(ITransaction tx, LoadoutItemGroupId[] groupIds);

    /// <summary>
    /// Removes the items from their Loadouts.
    /// </summary>
    ValueTask RemoveItems(LoadoutItemGroupId[] groupIds);

    /// <summary>
    /// Removes a collection.
    /// </summary>
    ValueTask RemoveCollection(LoadoutId loadoutId, CollectionGroupId collection);
}
