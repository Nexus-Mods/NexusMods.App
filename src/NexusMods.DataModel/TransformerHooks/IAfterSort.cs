using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A transformer hook which is executed after the sorting of mods.
/// </summary>
public interface IAfterSort
{
    /// <summary>
    /// This method is executed after the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public ValueTask<IEnumerable<Mod>> AfterSortAsync(IEnumerable<Mod> mods, Loadout loadout, CancellationToken ct);
}
