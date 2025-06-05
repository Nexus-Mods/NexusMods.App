using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;

public static class RedModExtensions
{
    public static RedModSortableEntry.ReadOnly[] RetrieveRedModSortableEntries(this IDb db, SortOrderId sortOrderId)
    {
        return RedModSortableEntry.All(db)
            .Where(si => si.IsValid() && si.AsSortableEntry().ParentSortOrderId == sortOrderId)
            .OrderBy(si => si.AsSortableEntry().SortIndex)
            .ToArray();
    }
}
