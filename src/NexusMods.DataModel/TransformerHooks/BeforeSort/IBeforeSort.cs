using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.TransformerHooks.BeforeSort;

/// <summary>
/// This interface is used for denoting a transformer hook which is executed before the sorting of mods.
/// </summary>
public interface IBeforeSort : IGameFilteringHook
{
    /// <summary>
    /// This method is executed before the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<Result> BeforeSortAsync(Mod mod, Loadout loadout, CancellationToken ct);
}
