using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.Library;
using NexusMods.MnemonicDB.Abstractions;

/// <summary>
/// Processes library items to determine potential warnings before deletion of
/// library items.
/// </summary>
/// <remarks>
/// This struct categorizes library items based on specific criteria to allow
/// for the generation 
/// 
/// The categories are:
/// 
/// - Non-permanent items: Downloads that are not guaranteed to be redownloadable (e.g., not from Nexus Mods).
/// - Manually added items: Items that were manually added from the file system to the library.
/// - Items in loadouts: Items that are currently part of one or more loadouts.
/// </remarks>
public record struct LibraryItemDeleteWarningDetector
{
    /// <summary>
    /// Gets the list of library items that are not guaranteed to be redownloadable.
    /// </summary>
    public List<LibraryItem.ReadOnly> NonPermanentItems { get; init; }

    /// <summary>
    /// Gets the list of library items that were manually added to the library.
    /// </summary>
    public List<LibraryItem.ReadOnly> ManuallyAddedItems { get; init; }

    /// <summary>
    /// Gets the list of library items that are currently part of one or more loadouts.
    /// </summary>
    public List<LibraryItem.ReadOnly> ItemsInLoadouts { get; init; }

    /// <summary>
    /// Processes a collection of library items and categorizes them based on
    /// deletion warning criteria.
    /// </summary>
    /// <param name="connection">The database connection used to retrieve additional information.</param>
    /// <param name="toRemove">The collection of library items to process.</param>
    /// <returns>A new instance of LibraryItemDeleteWarningProcessor containing the categorized items.</returns>
    public static LibraryItemDeleteWarningDetector Process(
        IConnection connection,
        IEnumerable<LibraryItem.ReadOnly> toRemove)
    {
        var loadouts = Loadout.All(connection.Db).ToArray();
        var nonPermanentItems = new List<LibraryItem.ReadOnly>();
        var manuallyAddedItems = new List<LibraryItem.ReadOnly>();
        var itemsInLoadouts = new List<LibraryItem.ReadOnly>();

        foreach (var item in toRemove)
        {
            var info = LibraryItemRemovalInfo.Determine(connection, item, loadouts);

            if (info.IsNonPermanent)
                nonPermanentItems.Add(item);

            if (info.IsManuallyAdded)
                manuallyAddedItems.Add(item);

            if (info.IsAddedToAnyLoadout)
                itemsInLoadouts.Add(item);
        }

        return new LibraryItemDeleteWarningDetector
        {
            NonPermanentItems = nonPermanentItems,
            ManuallyAddedItems = manuallyAddedItems,
            ItemsInLoadouts = itemsInLoadouts,
        };
    }
}
