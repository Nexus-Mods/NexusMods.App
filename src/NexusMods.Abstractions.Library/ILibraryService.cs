using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Models.Library;

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
    /// Removes a number of items from the library.
    /// This will automatically unlink the loadouts from the items are part of.
    /// </summary>
    /// <param name="libraryItems">The items to remove from the library.</param>
    /// <param name="gcRunMode">Defines how the garbage collector should be run</param>
    Task RemoveLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously);

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
/// Represents the result of a <see cref="ILibraryService.ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly,LibraryItem.ReadOnly,NexusMods.Abstractions.Library.ReplaceLibraryItemOptions,NexusMods.MnemonicDB.Abstractions.ITransaction)"/> operation.
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
/// Options for the <see cref="ILibraryService.ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly,LibraryItem.ReadOnly,NexusMods.Abstractions.Library.ReplaceLibraryItemOptions,NexusMods.MnemonicDB.Abstractions.ITransaction)"/>
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
