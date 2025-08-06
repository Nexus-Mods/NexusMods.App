using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;

public static class RedModExtensions
{
    public static RedModSortOrderItem.ReadOnly[] RetrieveRedModSortableEntries(this IDb db, SortOrderId sortOrderId)
    {
        return RedModSortOrderItem.All(db)
            .Where(si => si.IsValid() && si.AsSortOrderItem().ParentSortOrderId == sortOrderId)
            .OrderBy(si => si.AsSortOrderItem().SortIndex)
            .ToArray();
    }
}
