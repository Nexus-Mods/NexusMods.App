using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.Extensions;

public static class RedModExtensions
{
    public static RedModSortOrderItem.ReadOnly[] RetrieveRedModSortableEntries(this IDb db, SortOrderId sortOrderId)
    {
        return db.Datoms(SortOrderItem.ParentSortOrder, sortOrderId)
            .Select(d => RedModSortOrderItem.Load(db, d.E))
            .Where(si => si.IsValid())
            .OrderBy(si => si.AsSortOrderItem().SortIndex)
            .ToArray();
    }
    
    public static IReadOnlyList<SortItemData<SortItemKey<string>>> RetrieveRedModSortOrderItems(IDb db, SortOrderId sortOrderId)
    {
        return db.Connection.Query<(string FolderName, int SortIndex, EntityId ItemId)>($"""
            SELECT * FROM redmod.RedModSortOrderItems({db}, {sortOrderId.Value})
            """)
            .Select(row => new SortItemData<SortItemKey<string>>(
                new SortItemKey<string>(row.FolderName),
                row.SortIndex
            ))
            .ToList(); 
    }
}
