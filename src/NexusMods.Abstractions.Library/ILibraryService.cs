using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents the library, this class provides access to various functionalities
/// that are accessable from within a 'library' related view.
/// </summary>
[PublicAPI]
public interface ILibraryService
{
    /// <summary>
    /// Adds a download to the library.
    /// </summary>
    IJobTask<IAddDownloadJob, LibraryFile.ReadOnly> AddDownload(IJobTask<IDownloadJob, AbsolutePath> downloadJob);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    IJobTask<IAddLocalFile, LocalFile.ReadOnly> AddLocalFile(AbsolutePath absolutePath);
    
    /// <summary>
    /// Returns all loadouts that contain the given library item.
    /// </summary>
    /// <param name="libraryItem">The item to search for.</param>
    /// <param name="db">
    ///     The database instance to use. If not specified, will inherit from <see cref="LibraryItem.ReadOnly"/>.
    ///     Pass this parameter for latest snapshot, else pass null to use snapshot from <see cref="LibraryItem.ReadOnly"/>
    /// </param>
    IEnumerable<Loadout.ReadOnly> LoadoutsWithLibraryItem(LibraryItem.ReadOnly libraryItem, IDb? db = null);

    /// <summary>
    /// Adds a library file.
    /// </summary>
    Task<LibraryFile.New> AddLibraryFile(ITransaction transaction, AbsolutePath source);

    /// <summary>
    /// Installs a library item into a target loadout.
    /// To remove an item, use <see cref="RemoveLibraryItemFromLoadout(LoadoutItemId)"/>.
    /// </summary>
    /// <param name="libraryItem">The item to install.</param>
    /// <param name="targetLoadout">The target loadout.</param>
    /// <param name="parent">If specified the installed item will be placed in this group, otherwise it will default to the user's local collection</param>
    /// <param name="installer">The Library will use this installer to install the item</param>
    /// <param name="fallbackInstaller">Fallback installer instead of the default advanced installer</param>
    IJobTask<IInstallLoadoutItemJob, LoadoutItemGroup.ReadOnly> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null);

    /// <summary>
    /// Removes a number of items from the library.
    /// This will automatically unlink the loadouts from the items are part of.
    /// </summary>
    /// <param name="libraryItems">The items to remove from the library.</param>
    /// <param name="gcRunMode">Defines how the garbage collector should be run</param>
    Task RemoveItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously);

    /// <summary>
    /// Unlinks a single item added by <see cref="InstallItem"/> function call from a loadout.
    /// </summary>
    /// <param name="itemId">The item to remove from the loadout.</param>
    Task RemoveLibraryItemFromLoadout(LoadoutItemId itemId);

    /// <summary>
    /// Unlinks a number of items added by <see cref="InstallItem"/> function call from a loadout.
    /// </summary>
    /// <param name="itemIds">The items to remove from the loadout.</param>
    Task RemoveLibraryItemFromLoadout(IEnumerable<LoadoutItemId> itemIds);

    /// <summary>
    /// Unlinks a single item added by <see cref="InstallItem"/> function call from a loadout.
    /// </summary>
    /// <param name="itemId">The item to remove from the loadout.</param>
    /// <param name="tx">Existing transaction to use</param>
    void RemoveLibraryItemFromLoadout(LoadoutItemId itemId, ITransaction tx);

    /// <summary>
    /// Unlinks a number of items added by <see cref="InstallItem"/> function call from a loadout.
    /// </summary>
    /// <param name="itemIds">The items to remove</param>
    /// <param name="tx">Existing transaction to use</param>
    void RemoveLibraryItemFromLoadout(IEnumerable<LoadoutItemId> itemIds, ITransaction tx);

    /// <summary>
    /// Removes library items (originally installed via <see cref="InstallItem"/>) from all
    /// loadouts using an existing transaction
    /// </summary>
    /// <param name="libraryItems">The library items to remove from the loadouts</param>
    /// <param name="tx">Existing transaction to use</param>
    void RemoveLibraryItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems, ITransaction tx);

    /// <summary>
    /// Removes library items (originally installed via <see cref="InstallItem"/>) from all
    /// loadouts with automatic transaction
    /// </summary>
    /// <param name="libraryItems">The library items to remove from the loadouts</param>
    Task RemoveLibraryItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems);
}
