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
/// that are accessible from within a 'library' related view.
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
    /// <remarks>
    ///     The loadout and linked item to the current item.
    /// </remarks>
    IEnumerable<(Loadout.ReadOnly loadout, LibraryLinkedLoadoutItem.ReadOnly linkedItem)> LoadoutsWithLibraryItem(LibraryItem.ReadOnly libraryItem, IDb? db = null);

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
    /// <param name="fallbackInstaller">The installer to use if the default installer fails</param>
    /// <param name="transaction">The transaction to attach the installation to. Install is only completed when transaction is completed.</param>
    /// <remarks>
    /// Job returns null <see cref="LoadoutItemGroup.ReadOnly"/> after
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

    /// <summary>
    /// Replaces all occurrences of a library item with a new version in all loadouts
    /// </summary>
    /// <param name="oldItem">The library item to be replaced</param>
    /// <param name="newItem">The replacement library item</param>
    /// <param name="options">Options regarding how to replace this library item.</param>
    /// <param name="tx">The transaction to use</param>
    /// <returns>
    ///     If an error occurs at any step of the way, this returns a 'fail' enum.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLibraryItemInAllLoadouts(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem, ReplaceLibraryItemOptions options, ITransaction tx);

    /// <summary>
    /// Replaces all occurrences of a library item with a new version in all loadouts
    /// </summary>
    /// <param name="replacements">The replacements to perform</param>
    /// <param name="options">Options regarding how to replace these library items.</param>
    /// <param name="tx">The transaction to add this replace operation to.</param>
    /// <returns>
    ///     If an error occurs at any step of the way, this returns a 'fail' enum.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLibraryItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options, ITransaction tx);
    
    /// <summary>
    /// Replaces all occurrences of a library item with a new version in all loadouts
    /// </summary>
    /// <param name="replacements">The replacements to perform</param>
    /// <param name="options">Options regarding how to replace this library item.</param>
    /// <returns>
    ///     If an error occurs at any step of the way, this returns a 'fail' enum.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLibraryItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options);
}

/// <summary>
/// Represents the result of a <see cref="ILibraryService.ReplaceLibraryItemInAllLoadouts"/> operation.
/// </summary>
public enum LibraryItemReplacementResult
{
    /// <summary>
    /// The operation was successful.
    /// </summary>
    Success,

    /// <summary>
    /// The operation failed (unknown reason).
    /// </summary>
    FailedUnknownReason,
}

/// <summary>
/// Options for the <see cref="ILibraryService.ReplaceLibraryItemInAllLoadouts(NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,ReplaceLibraryItemOptions,NexusMods.MnemonicDB.Abstractions.ITransaction)"/>
/// API.
/// </summary>
public struct ReplaceLibraryItemOptions
{
    /// <summary>
    /// Ignores items in ReadOnly collections such as collections from Nexus Mods.
    /// </summary>
    public bool IgnoreReadOnlyCollections { get; set; }
}

/// <summary>
/// Options for the 'ReplaceLibraryItemsInAllLoadouts' API.
/// </summary>
public struct ReplaceLibraryItemsOptions
{
    /// <summary>
    /// Ignores items in ReadOnly collections such as collections from Nexus Mods.
    /// </summary>
    public bool IgnoreReadOnlyCollections { get; set; }
    
    /// <summary>
    /// Gets the <see cref="ReplaceLibraryItemOptions"/> for this <see cref="ReplaceLibraryItemsOptions"/>
    /// </summary>
    public ReplaceLibraryItemOptions ToReplaceLibraryItemOptions() => new() { IgnoreReadOnlyCollections = IgnoreReadOnlyCollections };
}
