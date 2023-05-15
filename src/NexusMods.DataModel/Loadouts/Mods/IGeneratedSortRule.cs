using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.TriggerFilter;

namespace NexusMods.DataModel.Loadouts.Mods;

/// <summary>
/// A sort rule that is generated whenever the mods in the loadout change.
/// </summary>
public interface IGeneratedSortRule
{
    public ITriggerFilter<ModId, Loadout> TriggerFilter { get; }

    public IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId modId, Loadout loadout);
}
