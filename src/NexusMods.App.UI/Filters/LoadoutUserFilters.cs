using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI;

/// <summary>
/// Filters for displaying the contents of a Loadout to the user.
/// </summary>
[PublicAPI]
public static class LoadoutUserFilters
{
    /// <summary>
    /// Returns an enumerable containing all items to be shown to the user.
    /// </summary>
    public static IEnumerable<LoadoutItem.ReadOnly> GetItems(Loadout.ReadOnly loadout)
    {
        var db = loadout.Db;
        var loadoutId = loadout.LoadoutId;

        var libraryLinkedLoadoutItems = db.Datoms(LibraryLinkedLoadoutItem.LibraryItem).AsModels<LoadoutItem.ReadOnly>(db);
        foreach (var entity in libraryLinkedLoadoutItems)
        {
            if (entity.LoadoutId != loadoutId) continue;
            if (!entity.IsLoadoutItemGroup()) continue;
            if (!LibraryUserFilters.ShouldShow(entity.ToLibraryLinkedLoadoutItem().LibraryItem)) continue;

            yield return entity;
        }
    }
}
