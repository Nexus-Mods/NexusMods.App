using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// Called after the flattening of mods into a single collection but before the
/// creation of the apply plan.
/// </summary>
public interface IBeforeMakeApplyPlan
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
    public ValueTask<Dictionary<GamePath, (AModFile File, Mod Mod)>> BeforeMakeApplyPlan(Dictionary<GamePath, (AModFile File, Mod Mod)> files, Loadout loadout, CancellationToken ct);
    
}
