using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.TransformerHooks.BeforeMakeApplyPlan;
using NexusMods.DataModel.TransformerHooks.BeforeSort;
using NexusMods.Paths;

namespace NexusMods.DataModel.TransformerHooks;

/// <summary>
/// A registry of transformer hooks, which are executed at various points during datamodel
/// operations.
/// </summary>
public class TransformerRegistry
{
    private readonly Lazy<ILookup<GameDomain, IBeforeSort>> _beforeSort;
    private readonly Lazy<ILookup<(GameDomain, GamePath), IBeforeMakeApplyPlan>> _beforeMakeApplyPlan;
    private readonly Lazy<IEnumerable<IBeforeSort>> _beforeSortNoDomain;

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="provider"></param>
    public TransformerRegistry(IServiceProvider provider)
    {
        
        _beforeMakeApplyPlan =
            provider
                .GetIndexedServicesLazily<IBeforeMakeApplyPlan, (GameDomain,
                    GamePath)>(x => (from gameDomain in x.GameDomains
                    from file in x.Files
                    select (gameDomain, file)));
        _beforeSort = provider.GetIndexedServicesLazily<IBeforeSort, GameDomain>(x => x.GameDomains);
        _beforeSortNoDomain = provider.GetServicesLazily<IBeforeSort>(s => !s.GameDomains.Any());
    }

    /// <summary>
    /// This method is executed before the sorting of mods.
    /// </summary>
    /// <param name="mods"></param>
    /// <param name="loadout"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<Mod> BeforeSort(IEnumerable<Mod> mods, Loadout loadout, 
        [EnumeratorCancellation] CancellationToken token)
    {
        var xforms = _beforeSort.Value[loadout.Installation.Game.Domain]
            .Concat(_beforeSortNoDomain.Value);
        
        foreach (var originalMod in mods)
        {
            var mod = originalMod;
            var disabled = false;
            
            foreach (var xform in xforms)
            {
                var result = await xform.BeforeSortAsync(mod, loadout, token);
                if (result is NoChanges)
                {
                    continue;
                }
                else if (result is DisableMod)
                {
                    disabled = true;
                    break;
                }
                else if (result is ReplaceRules replace)
                {
                    mod = mod with { SortRules = replace.Rules.ToImmutableList() };
                }
            }

            if (!disabled)
            {
                yield return mod;
            }
        }
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
            Loadout loadout, LoadoutManager manager, CancellationToken token)
    {
        var domain = loadout.Installation.Game.Domain;

        var changeList = new List<(AModFile File, Mod Mod)>();
        
        foreach (var kv in files)
        {
            foreach (var xform in _beforeMakeApplyPlan.Value[(domain, kv.Key)])
            {
                var result = await xform.BeforeMakeApplyPlan(kv.Value.File, files, loadout, token);
                switch (result)
                {
                    case Nothing:
                        continue;
                    case SetSizeAndHash setSizeAndHash:
                        var file = (AStaticModFile)kv.Value.File;
                        file = file with
                        {
                            Size = setSizeAndHash.Size ?? file.Size,
                            Hash = setSizeAndHash.Hash ?? file.Hash
                        };
                        files[kv.Key] = (file, kv.Value.Mod);
                        changeList.Add((kv.Value.File, kv.Value.Mod));
                        break;
                }
            }
        }
        
        if (changeList.Count > 0)
        {
            foreach (var entry in changeList)
            {
                files[entry.File.To] = entry;
            }
            manager.ReplaceFiles(loadout.LoadoutId, changeList.Select(pair => (pair.File, pair.Mod.Id)),
                "Changes from Before Apply Plan Transformer");
        }
        
        return files;
    }
}
