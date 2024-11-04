using NexusMods.Abstractions.Games;
using NexusMods.Games.RedEngine.Cyberpunk2077.Models;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class RedModSortableItem(RedModSortableItemProvider provider, RedModLoadoutGroup.ReadOnly group) : ISortableItem
{
    public ISortableItemProvider Provider => provider;
    public int SortIndex => (int)group.Rebase().SortIndex;
    
    public EntityId EntityId => group.Id;
    
    
    public async Task SetRelativePosition(int delta)
    {
        await provider.SetRelativePosition(group, delta);
    }
    
    /// <summary>
    /// Used for testing purposes
    /// </summary>
    internal string Name => RedModDeployTool.RedModFolder(group).Name;
}
