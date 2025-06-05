using DynamicData.Kernel;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;

public class RedModSortableItem : ISortableItem<RedModSortableItem, SortItemKey<string>>
{
    public RedModSortableItem(int sortIndex, RelativePath redModFolderName, string modName, bool isActive)
    {
        SortIndex = sortIndex;
        RedModFolderName = redModFolderName;
        DisplayName = redModFolderName.ToString();
        ModName = modName;
        IsActive = isActive;
        Key = new SortItemKey<string>(redModFolderName);
    }
    
    public RelativePath RedModFolderName { get; set; }

    public SortItemKey<string> Key { get; }

    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public Optional<LoadoutItemGroupId> ModGroupId { get; set; }
    public bool IsActive { get; set; }
    public ISortableItemLoadoutData? LoadoutData { get; set; }
}
