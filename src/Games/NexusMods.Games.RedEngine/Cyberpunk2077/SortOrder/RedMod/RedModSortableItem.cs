using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItem : ISortableItem, IComparable<RedModSortableItem>
{
    public RedModSortableItem(RedModSortableItemProvider provider, int sortIndex, RelativePath redModFolderName, string modName, bool isActive, Guid itemId)
    {
        SortableItemProvider = provider;
        SortIndex = sortIndex;
        RedModFolderName = redModFolderName;
        DisplayName = redModFolderName.ToString();
        ModName = modName;
        IsActive = isActive;
        ItemId = itemId;
    }
    
    public RelativePath RedModFolderName { get; set; }

    public ILoadoutSortableItemProvider SortableItemProvider { get; }
    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public bool IsActive { get; set; }
    
    public Guid ItemId { get; }

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
