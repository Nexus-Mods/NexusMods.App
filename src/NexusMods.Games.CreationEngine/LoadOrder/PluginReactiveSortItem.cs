using DynamicData.Kernel;
using Mutagen.Bethesda.Plugins;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Games.CreationEngine.LoadOrder;

public class PluginReactiveSortItem : IReactiveSortItem<PluginReactiveSortItem, SortItemKey<ModKey>>
{
    public PluginReactiveSortItem(int sortIndex, ModKey modName, string parentName, bool isActive)
    {
        SortIndex = sortIndex;
        ModName = parentName;
        DisplayName = modName.ToString();
        IsActive = isActive;
        Key = new SortItemKey<ModKey>(modName);
    }
    
    public SortItemKey<ModKey> Key { get; }
    public int SortIndex { get; set; }
    public string DisplayName { get; }
    public string ModName { get; set; }
    public Optional<LoadoutItemGroupId> ModGroupId { get; set; }
    public bool IsActive { get; set; }
    public ISortItemLoadoutData? LoadoutData { get; set; }
}
