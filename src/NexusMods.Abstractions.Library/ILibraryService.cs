using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;

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
    /// <remarks>
    ///     The loadout and linked item to the current item.
    /// </remarks>
    IEnumerable<(Loadout.ReadOnly loadout, LibraryLinkedLoadoutItem.ReadOnly linkedItem)> LoadoutsWithLibraryItem(LibraryItem.ReadOnly libraryItem);

    /// <summary>
    /// Returns all unique loadouts that contain any of the given library items.
    /// </summary>
    /// <param name="libraryItems">The items to search for.</param>
    /// <remarks>
    ///     Returns a dictionary where each key is a loadout and the value is a list of 
    ///     tuples containing the library items found in that loadout and their linked items.
    /// </remarks>
    IReadOnlyDictionary<Loadout.ReadOnly, IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>> LoadoutsWithLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems);

    /// <summary>
    /// Returns all collections that contain the given library item.
    /// </summary>
    /// <param name="libraryItem">The item to search for.</param>
    /// <param name="excludeReadOnlyCollections">If true, filters out read-only collections such as collections from Nexus Mods.</param>
    /// <remarks>
    ///     Returns tuples containing the collection and the linked item within that collection.
    /// </remarks>
    IEnumerable<(CollectionGroup.ReadOnly collection, LibraryLinkedLoadoutItem.ReadOnly linkedItem)> CollectionsWithLibraryItem(LibraryItem.ReadOnly libraryItem, bool excludeReadOnlyCollections = false);

    /// <summary>
    /// Returns all unique collections that contain any of the given library items.
    /// </summary>
    /// <param name="libraryItems">The items to search for.</param>
    /// <param name="excludeReadOnlyCollections">If true, filters out read-only collections such as collections from Nexus Mods.</param>
    /// <remarks>
    ///     Returns a dictionary where each key is a collection and the value is a list of 
    ///     tuples containing the library items found in that collection and their linked items.
    /// </remarks>
    IReadOnlyDictionary<CollectionGroup.ReadOnly, IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>> CollectionsWithLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, bool excludeReadOnlyCollections = false);

    /// <summary>
    /// Adds a library file.
    /// </summary>
    Task<LibraryFile.New> AddLibraryFile(ITransaction transaction, AbsolutePath source);

    /// <summary>
    /// Installs a library item into a target loadout.
    /// To remove an installed item, use <see cref="RemoveLinkedItemFromLoadout(LibraryLinkedLoadoutItemId)"/>.
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
    /// Removes a number of items from the library.
    /// This will automatically unlink the loadouts from the items are part of.
    /// </summary>
    /// <param name="libraryItems">The items to remove from the library.</param>
    /// <param name="gcRunMode">Defines how the garbage collector should be run</param>
    Task RemoveLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously);

    /// <summary>
    /// Removes a single linked loadout item from its loadout,
    /// managing the transaction automatically.
    /// </summary>
    /// <param name="itemId">The ID of the linked loadout item to remove from the loadout.</param>
    Task RemoveLinkedItemFromLoadout(LibraryLinkedLoadoutItemId itemId);

    /// <summary>
    /// Removes multiple linked loadout items from their loadout,
    /// managing the transaction automatically.
    /// </summary>
    /// <param name="itemIds">The IDs of the linked loadout items to remove from their loadout.</param>
    Task RemoveLinkedItemsFromLoadout(IEnumerable<LibraryLinkedLoadoutItemId> itemIds);

    /// <summary>
    /// Removes a single linked loadout item from a loadout,
    /// using the provided transaction.
    /// </summary>
    /// <param name="itemId">The ID of the linked loadout item to remove from its loadout.</param>
    /// <param name="tx">Existing transaction to use for this operation.</param>
    void RemoveLinkedItemFromLoadout(LibraryLinkedLoadoutItemId itemId, ITransaction tx);

    /// <summary>
    /// Removes multiple linked loadout items from their loadout,
    /// using the provided transaction.
    /// </summary>
    /// <param name="itemIds">The IDs of the linked loadout items to remove from their loadout.</param>
    /// <param name="tx">Existing transaction to use for this operation.</param>
    void RemoveLinkedItemsFromLoadout(IEnumerable<LibraryLinkedLoadoutItemId> itemIds, ITransaction tx);

    /// <summary>
    /// Removes all linked loadout items from all loadouts,
    /// using the provided transaction.
    /// </summary>
    /// <param name="libraryItems">The library items whose associated linked loadout items should be removed.</param>
    /// <param name="tx">Existing transaction to use for this operation.</param>
    void RemoveLinkedItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems, ITransaction tx);

    /// <summary>
    /// Removes all linked loadout items from all loadouts,
    /// managing the transaction automatically.
    /// </summary>
    /// <param name="libraryItems">The library items whose associated linked loadout items should be removed.</param>
    Task RemoveLinkedItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems);

    /// <summary>
    /// Replaces linked loadout items across all loadouts with installations of a different library item.   
    /// </summary>
    /// <param name="oldItem">The library item whose linked loadout items should be replaced.</param>
    /// <param name="newItem">The replacement library item from which to install the new linked loadout items from.</param>
    /// <param name="options">Options controlling how to replace the linked loadout items.</param>
    /// <param name="tx">The transaction to use for this operation.</param>
    /// <returns>
    ///     A result indicating success or failure of the replacement operation.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem, ReplaceLibraryItemOptions options, ITransaction tx);

    /// <summary>
    /// Replaces linked loadout items across all loadouts with installations of a different library item.   
    /// </summary>
    /// <param name="oldItem">The library item whose linked loadout items should be replaced.</param>
    /// <param name="newItem">The replacement library item from which to install the new linked loadout items from.</param>
    /// <param name="options">Options controlling how to replace the linked loadout items.</param>
    /// <returns>
    ///     A result indicating success or failure of the replacement operation.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem, ReplaceLibraryItemOptions options);
    
    /// <summary>
    /// Replaces multiple sets of linked loadout items across all loadouts with new versions.
    /// </summary>
    /// <param name="replacements">The pairs of library items (old and new) whose linked loadout items should be replaced.</param>
    /// <param name="options">Options controlling how to replace the linked loadout items.</param>
    /// <param name="tx">The transaction to use for this operation.</param>
    /// <returns>
    ///     A result indicating success or failure of the replacement operation.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options, ITransaction tx);
    
    /// <summary>
    /// Replaces multiple sets of linked loadout items across all loadouts with new versions,
    /// managing the transaction automatically.
    /// </summary>
    /// <param name="replacements">The pairs of library items (old and new) whose linked loadout items should be replaced.</param>
    /// <param name="options">Options controlling how to replace the linked loadout items.</param>
    /// <returns>
    ///     A result indicating success or failure of the replacement operation.
    /// </returns>
    ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options);
}

/// <summary>
/// Represents the result of a <see cref="ILibraryService.ReplaceLinkedItemsInAllLoadouts(NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,NexusMods.Abstractions.Library.ReplaceLibraryItemOptions,NexusMods.MnemonicDB.Abstractions.ITransaction)"/> operation.
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
/// Options for the <see cref="ILibraryService.ReplaceLinkedItemsInAllLoadouts(NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,NexusMods.Abstractions.Library.Models.LibraryItem.ReadOnly,NexusMods.Abstractions.Library.ReplaceLibraryItemOptions,NexusMods.MnemonicDB.Abstractions.ITransaction)"/>
/// API.
/// </summary>
public struct ReplaceLibraryItemOptions
{
    /// <summary>
    /// Skips items in ReadOnly collections such as collections from Nexus Mods.
    /// </summary>
    public bool IgnoreReadOnlyCollections { get; set; }
}

/// <summary>
/// Options controlling how multiple sets of linked loadout items are replaced across loadouts.
/// </summary>
public struct ReplaceLibraryItemsOptions
{
    /// <summary>
    /// Skips items in ReadOnly collections such as collections from Nexus Mods.
    /// </summary>
    public bool IgnoreReadOnlyCollections { get; set; }
    
    /// <summary>
    /// Gets the <see cref="ReplaceLibraryItemOptions"/> for this <see cref="ReplaceLibraryItemsOptions"/>
    /// </summary>
    public ReplaceLibraryItemOptions ToReplaceLibraryItemOptions() => new() { IgnoreReadOnlyCollections = IgnoreReadOnlyCollections };
}
