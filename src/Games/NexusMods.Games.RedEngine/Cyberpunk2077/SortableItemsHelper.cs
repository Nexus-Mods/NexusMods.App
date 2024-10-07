using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class SortableItemsHelper
{
    public static uint NextSortIndex(Loadout.ReadOnly loadout)
    {
        uint max = 1;
        foreach (var item in loadout.Items)
        {
            if (!RedModLoadoutGroup.SortIndex.TryGet(item, out var index)) 
                continue;
            
            if (index > max)
            {
                max = index;
            }
        }

        return max + 1;
    }
}
