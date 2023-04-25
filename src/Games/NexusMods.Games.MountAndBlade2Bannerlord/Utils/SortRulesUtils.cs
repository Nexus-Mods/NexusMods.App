using System.Collections.Immutable;
using Bannerlord.ModuleManager;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Sorting.Rules;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Utils;

public static class SortRulesUtils
{
    private static ModId GetModIdFromModuleId(string moduleId)
    {
        // No idea
        return ModId.New();
    }

    public static ImmutableList<ISortRule<Mod, ModId>> GetSortRules(ModuleInfoExtended moduleInfo)
    {
        var sortRulesBuilder = ImmutableList.CreateBuilder<ISortRule<Mod, ModId>>();
        sortRulesBuilder.AddRange(moduleInfo.DependenciesLoadBeforeThisDistinct().Select(x => new Before<Mod, ModId>(GetModIdFromModuleId(x.Id))));
        sortRulesBuilder.AddRange(moduleInfo.DependenciesLoadBeforeThisDistinct().Select(x => new After<Mod, ModId>(GetModIdFromModuleId(x.Id))));
        return sortRulesBuilder.ToImmutable();
    } 
}