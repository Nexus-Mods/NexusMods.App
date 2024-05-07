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
    public new Task<IEnumerable<Mod.Model>> SortMods(Loadout.Model loadout) => base.SortMods(loadout);

    public override async ValueTask<ISortRule<Mod.Model, ModId>[]> ModSortRules(Loadout.Model loadout, Mod.Model mod)
    {
        if (mod.GetModuleInfo() is { } moduleInfo)
            return await GetRules(moduleInfo.FromEntity(), loadout).ToArrayAsync();
        else
            return [];
    }

    private static async IAsyncEnumerable<ISortRule<Mod.Model, ModId>> GetRules(ModuleInfoExtended moduleInfo, Loadout.Model loadout)
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
                yield return new After<Mod.Model, ModId> { Other = modId };
            }
        }
        foreach (var moduleMetadata in moduleInfo.DependenciesLoadAfterThisDistinct())
        {
            if (GetModIdFromModuleId(moduleMetadata.Id) is { } modId)
            {
                yield return new Before<Mod.Model, ModId> { Other = modId };
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
