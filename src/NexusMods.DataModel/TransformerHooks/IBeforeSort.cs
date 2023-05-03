using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// This interface is used for denoting a transformer hook which is executed before the sorting of mods.
/// </summary>
public interface IBeforeSort
{
    /// <summary>
    /// This method is executed before the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<Mod>> BeforeSortAsync(IEnumerable<Mod> mods, Loadout loadout, CancellationToken ct);
}
