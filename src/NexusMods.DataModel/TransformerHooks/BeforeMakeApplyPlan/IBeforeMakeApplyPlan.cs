using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks.BeforeMakeApplyPlan;

/// <summary>
/// Called after the flattening of mods into a single collection but before the
/// creation of the apply plan.
/// </summary>
public interface IBeforeMakeApplyPlan : IFileFilteringHook
{
    /// <summary>
    /// This method is executed after the flattening of mods into a single collection but before the
    /// creation of the apply plan.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<Result> BeforeMakeApplyPlan(AModFile file, IReadOnlyDictionary<GamePath, (AModFile File, Mod Mod)> flattenedList,
        Loadout loadout, CancellationToken ct);
    
}
