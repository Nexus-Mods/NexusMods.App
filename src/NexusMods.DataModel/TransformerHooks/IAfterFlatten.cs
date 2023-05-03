using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A transformer hook which is executed after the flattening of mods into a single collection.
/// </summary>
public interface IAfterFlatten
{
    /// <summary>
    /// Called after the flattening of mods into a single collection.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<Dictionary<GamePath, (AModFile File, Mod Mod)>> AfterFlattenAsync(Dictionary<GamePath, (AModFile File, Mod Mod)> files, IEnumerable<Mod> mods, Loadout loadout, CancellationToken ct);
}
