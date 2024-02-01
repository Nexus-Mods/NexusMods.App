using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Triggers;

namespace NexusMods.Abstractions.Loadouts.Sorting;

/// <summary>
/// A sort rule that is generated whenever the mods in the loadout change.
/// </summary>
public interface IGeneratedSortRule
{
    /// <summary>
    /// A trigger filter that determines when `GenerateSortRules` should be called. When the fingerprint changes
    /// the`GenerateSortRules` method will be called during sorting.
    /// </summary>
    public ITriggerFilter<ModId, Loadout> TriggerFilter { get; }

    /// <summary>
    /// Called to reduce this sort rule to one or more concrete sort rules.
    /// </summary>
    /// <param name="modId"></param>
    /// <param name="loadout"></param>
    /// <returns></returns>
    public IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId modId, Loadout loadout);
}
