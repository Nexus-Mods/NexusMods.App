using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;

public static class RedModCollectionExtensions
{
    public static List<RedModWithState> GetRedModsWithState(
        this IDb db, LoadoutId loadoutId, SortOrderId sortOrderId)
    {
        var existingEntries = db.GetPersistentEntries(sortOrderId);
        var entries =  RedModLoadoutGroup.All(db).Where(x => x.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId == loadoutId)
            .SelectMany<RedModLoadoutGroup.ReadOnly, RedModWithState>(g => {
                var redModEntries = existingEntries.Where(x => x.RedModFolderName.Equals(g.RedModFolder()));
                var modEntries = redModEntries.ToList();
                if (!modEntries.Any())
                {
                    return
                    [
                        // Eww
                        new RedModWithState(g, g.RedModFolder(), g.IsEnabled(), Guid.NewGuid(), Optional<SortableEntryId>.None),
                    ];
                }
                return modEntries
                    .Where(x => x.AsSortableEntry().ParentSortOrderId == sortOrderId)
                    .Select(x => 
                    {
                        var itemId = x.AsSortableEntry().ItemId;
                        return new RedModWithState(g, g.RedModFolder(), g.IsEnabled(), itemId, x.AsSortableEntry().SortableEntryId);
                    })
                    .OrderBy(x => x.SortIndex);
            })
            .ToList();
        return entries!;
    }
    
    public static RedModSortableEntry.ReadOnly[] GetPersistentEntries(this IDb db, SortOrderId sortOrderId)
    {
        return RedModSortableEntry.All(db)
            .Where(si => si.IsValid() && si.AsSortableEntry().ParentSortOrderId == sortOrderId)
            .OrderBy(si => si.AsSortableEntry().SortIndex)
            .ToArray();
    }

    public static LoadoutId GetLoadoutId(this RedModWithState redModWithState)
    {
        return redModWithState.RedMod.AsLoadoutItemGroup().AsLoadoutItem().LoadoutId;
    }
}
