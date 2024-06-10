using System.Diagnostics;
using Bannerlord.ModuleManager;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public sealed class MountAndBlade2BannerlordLoadoutSynchronizer(IServiceProvider provider) : ALoadoutSynchronizer(provider)
{
    public new Task<IEnumerable<Mod.ReadOnly>> SortMods(Loadout.ReadOnly loadout) => base.SortMods(loadout);

    public override async ValueTask<ISortRule<Mod.ReadOnly, ModId>[]> ModSortRules(Loadout.ReadOnly loadout, Mod.ReadOnly mod)
    {
        if (mod.GetModuleInfo() is { } moduleInfo)
            return await GetRules(moduleInfo.FromEntity(), loadout).ToArrayAsync();
        else
            return [];
    }

    private static async IAsyncEnumerable<ISortRule<Mod.ReadOnly, ModId>> GetRules(ModuleInfoExtended moduleInfo, Loadout.ReadOnly loadout)
    {

        ModId? GetModIdFromModuleId(string moduleId)
        {
            return loadout.Mods.FirstOrDefault(x => x.GetModuleInfo() is { } mi && mi.ModuleId == moduleId)?.ModId;
        }

        await Task.Yield();

        foreach (var moduleMetadata in moduleInfo.DependenciesLoadBeforeThisDistinct())
        {
            if (GetModIdFromModuleId(moduleMetadata.Id) is { } modId)
            {
                yield return new After<Mod.ReadOnly, ModId> { Other = modId };
            }
        }
        foreach (var moduleMetadata in moduleInfo.DependenciesLoadAfterThisDistinct())
        {
            if (GetModIdFromModuleId(moduleMetadata.Id) is { } modId)
            {
                yield return new Before<Mod.ReadOnly, ModId> { Other = modId };
            }
        }
        foreach (var moduleMetadata in moduleInfo.DependenciesIncompatiblesDistinct())
        {
            if (GetModIdFromModuleId(moduleMetadata.Id) is { } modId)
            {
                // If an incompatible module was detected, the dependency rules were not respected
                throw new UnreachableException();
            }
        }
    }
}
