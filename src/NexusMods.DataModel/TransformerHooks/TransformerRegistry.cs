using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A registry of transformer hooks, which are executed at various points during datamodel
/// operations.
/// </summary>
public class TransformerRegistry
{
    private readonly Lazy<IEnumerable<IBeforeSort>> _beforeSort;
    private readonly Lazy<IEnumerable<IAfterSort>> _afterSort;
    private readonly Lazy<IEnumerable<IAfterFlatten>> _afterFlatten;
    private readonly Lazy<IEnumerable<IBeforeMakeApplyPlan>> _beforeMakeApplyPlan;

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="provider"></param>
    public TransformerRegistry(IServiceProvider provider)
    {
        _afterFlatten = provider.GetServicesLazily<IAfterFlatten>();
        _afterSort = provider.GetServicesLazily<IAfterSort>();
        _beforeMakeApplyPlan = provider.GetServicesLazily<IBeforeMakeApplyPlan>();
        _beforeSort = provider.GetServicesLazily<IBeforeSort>();
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
        foreach (var xform in _beforeSort.Value)
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
        foreach (var xform in _afterSort.Value)
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
        foreach (var xform in _afterFlatten.Value)
        {
            files = await xform.AfterFlattenAsync(files, mods, loadout, token);
        }
        return files;
    }

    /// <summary>
    /// This method is executed after the flattening of mods into a single collection but before the
    /// the creation of the apply plan.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async ValueTask<Dictionary<GamePath, (AModFile File, Mod Mod)>> 
        BeforeMakeApplyPlan(Dictionary<GamePath, (AModFile File, Mod Mod)> files,
            Loadout loadout, CancellationToken token)
    {
        foreach (var xform in _beforeMakeApplyPlan.Value)
        {
            files = await xform.BeforeMakeApplyPlan(files, loadout, token);
        }
        return files;
    }
}
