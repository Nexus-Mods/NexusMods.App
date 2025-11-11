using System.Runtime.InteropServices;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Library;

namespace NexusMods.App.UI.Pages.Library;

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
/// - All Items: All items to be displayed.
/// - All Loadouts: All Loadouts that use the mods listed in All Items (including nested collections).
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
    /// All the items checked, regardless of whether they have warnings or not.
    /// </summary>
    public List<LibraryItem.ReadOnly> AllItems { get; init; }
    
    /// <summary>
    /// All loadouts which have been used.
    /// </summary>
    public List<LibraryItemUsedLoadoutInfo> LoadoutsUsed { get; init; }

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
        var gameToLoadoutRefCount = new Dictionary<EntityId, int>();
        foreach (var loadout in loadouts)
        {
            var gameInfo = loadout.InstallationInstance;
            ref var refCount = ref CollectionsMarshal.GetValueRefOrAddDefault(gameToLoadoutRefCount, gameInfo.GameMetadataId, out var exists);
            if (exists)
                refCount++;
            else
                refCount = 1;
        }
        
        var nonPermanentItems = new List<LibraryItem.ReadOnly>();
        var manuallyAddedItems = new List<LibraryItem.ReadOnly>();
        var itemsInLoadouts = new List<LibraryItem.ReadOnly>();
        var allItems = new List<LibraryItem.ReadOnly>();
        
        // Note: We don't track collections right now, but for when we do,
        //       this dict will be useful.
        var usedLoadouts = new Dictionary<EntityId, LibraryItemUsedLoadoutInfo>();

        foreach (var item in toRemove)
        {
            var info = LibraryItemRemovalInfo.Determine(item, loadouts);

            if (info.IsNonPermanent)
                nonPermanentItems.Add(item);

            if (info.IsManuallyAdded)
                manuallyAddedItems.Add(item);

            if (info.Loadouts.Length > 0)
                itemsInLoadouts.Add(item);

            // Add the loadout info.
            foreach (var loadout in info.Loadouts)
            {
                if (usedLoadouts.ContainsKey(loadout.LoadoutId)) 
                    continue;

                var installation = loadout.InstallationInstance;
                gameToLoadoutRefCount.TryGetValue(installation.GameMetadataId, out var refCount);
                usedLoadouts[loadout.LoadoutId] = new LibraryItemUsedLoadoutInfo()
                {
                    GameName = installation.Game.DisplayName,
                    LoadoutName = loadout.Name,
                    IsOnlyLoadoutForGame = refCount <= 1,
                };
            }
            
            allItems.Add(item);
        }

        return new LibraryItemDeleteWarningDetector
        {
            NonPermanentItems = nonPermanentItems,
            ManuallyAddedItems = manuallyAddedItems,
            ItemsInLoadouts = itemsInLoadouts,
            AllItems = allItems,
            LoadoutsUsed = usedLoadouts.Values.ToList(),
        };
    }
}
