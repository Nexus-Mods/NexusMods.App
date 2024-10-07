using System.Collections.ObjectModel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;


public class RedModSortableItemProvider : ISortableItemProvider
{
    protected readonly IConnection Connection;
    
    public RedModSortableItemProvider(IConnection connection)
    {
        Connection = connection;
    }

    public IEnumerable<ISortableItem> GetItems(Loadout.ReadOnly loadout)
    {
        return loadout.Items
            .OfTypeLoadoutItemGroup()
            .OfTypeRedModLoadoutGroup()
            .Select(group => new RedModSortableItem(this, group));
    }

    public async Task SetRelativePosition(RedModLoadoutGroup.ReadOnly group, int delta)
    {
        // Get all red mod groups for the loadout
        var groups = group.AsLoadoutItemGroup().AsLoadoutItem().Loadout
            .Items
            .OfTypeLoadoutItemGroup()
            .OfTypeRedModLoadoutGroup()
            .OrderBy(g => g.SortIndex)
            .Select(g => (g.SortIndex, g.Id))
            .ToList();
        
        // Get the current index of the group relative to the full list
        var currentIndex = groups.IndexOf((group.SortIndex, group.Id));
        // Get the new index of the group relative to the full list
        var newIndex = currentIndex + delta;
        
        // Ensure the new index is within the bounds of the list
        if (newIndex < 0 || newIndex >= groups.Count)
            return;
        
        // Remove the group from the list and insert it at the new index
        groups.RemoveAt((int)currentIndex);
        groups.Insert((int)newIndex, (group.SortIndex, group.Id));
        
        using var tx = Connection.BeginTransaction();
        
        foreach (var (g, idx) in groups.Select((g, idx) => (g, idx)))
        {
            if (g.SortIndex == idx)
                continue;
            
            tx.Add(g.Id, RedModLoadoutGroup.SortIndex, (uint)idx);
        }
        
        await tx.Commit();
    }
}
