using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItem : ISortableItem, IComparable<RedModSortableItem>
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
    
    public ISortItemKey Key { get; }

    public ILoadoutSortableItemProvider SortableItemProvider { get; }
    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public bool IsActive { get; set; }

    public int CompareTo(RedModSortableItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }

    public int CompareTo(ISortableItem? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return SortIndex.CompareTo(other.SortIndex);
    }
}
