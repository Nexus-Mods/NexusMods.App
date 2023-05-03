using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A registry of transformer hooks, which are executed at various points during datamodel
/// operations.
/// </summary>
public class TransformerRegistry
{
    private readonly IEnumerable<IBeforeSort> _beforeSort;
    private readonly IEnumerable<IAfterSort> _afterSort;
    private readonly IEnumerable<IAfterFlatten> _afterFlatten;

    public TransformerRegistry(IServiceProvider provider)
    {
        _afterFlatten = provider.GetServices<IAfterFlatten>();
        _afterSort = provider.GetServices<IAfterSort>();
        _beforeSort = provider.GetServices<IBeforeSort>();
    }

    /// <summary>
    /// This method is executed before the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<IEnumerable<Mod>> BeforeSort(IEnumerable<Mod> mods, Loadout loadout, CancellationToken token)
    {
        foreach (var xform in _beforeSort)
        {
            mods = await xform.BeforeSortAsync(mods, loadout, token);
        }
        return mods;
    }
    
    /// <summary>
    /// This method is executed after the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<IEnumerable<Mod>> AfterSort(IEnumerable<Mod> mods, Loadout loadout, CancellationToken token)
    {
        foreach (var xform in _afterSort)
        {
            mods = await xform.AfterSortAsync(mods, loadout, token);
        }
        return mods;
    }

    /// <summary>
    /// This method is executed after the sorting of mods.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<Dictionary<GamePath, (AModFile File, Mod Mod)>> AfterFlattenAsync(Dictionary<GamePath, (AModFile File, Mod Mod)> files, 
        IEnumerable<Mod> mods, Loadout loadout, CancellationToken token)
    {
        foreach (var xform in _afterFlatten)
        {
            files = await xform.AfterFlattenAsync(files, mods, loadout, token);
        }
        return files;
    }
}
