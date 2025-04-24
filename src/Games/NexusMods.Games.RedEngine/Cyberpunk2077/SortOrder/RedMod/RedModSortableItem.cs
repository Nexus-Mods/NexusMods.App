using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItem : ISortableItem<RedModSortableItem, SortItemKey<string>>
{
    public RedModSortableItem(RedModSortableItemProvider provider, int sortIndex, RelativePath redModFolderName, string modName, bool isActive)
    {
        SortableItemProvider = provider;
        SortIndex = sortIndex;
        RedModFolderName = redModFolderName;
        DisplayName = redModFolderName.ToString();
        ModName = modName;
        IsActive = isActive;
        Key = new SortItemKey<string>(redModFolderName);
    }
    
    public RelativePath RedModFolderName { get; set; }

    public SortItemKey<string> Key { get; }

    public ILoadoutSortableItemProvider<RedModSortableItem, SortItemKey<string>> SortableItemProvider { get; }
    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public bool IsActive { get; set; }
}
