using System.Diagnostics;
using Bannerlord.ModuleManager;
using JetBrains.Annotations;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Triggers;
using NexusMods.Games.MountAndBlade2Bannerlord.Extensions;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Sorters;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.Sorters.ModuleInfoSort")]
public class ModuleInfoSort : ISortRule<Mod.Model, ModId>, ITriggerFilter<ModId, Loadout.Model>
{
    public ITriggerFilter<ModId, Loadout.Model> TriggerFilter => this;

    private static IAsyncEnumerable<ISortRule<Mod.Model, ModId>> GetRules(ModuleInfoExtended moduleInfo, Loadout.Model loadout)
    {
        throw new NotImplementedException();
        /*
        ModId? GetModIdFromModuleId(string moduleId) => loadout.Mods.FirstOrDefault(x => x.GetModuleInfo() is { } mi && mi.Id == moduleId)?.Id;

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
        */
    }

    public IAsyncEnumerable<ISortRule<Mod.Model, ModId>> GenerateSortRules(ModId selfId, Loadout.Model loadout)
    {
        var thisMod = loadout[selfId];
        return thisMod.GetModuleInfo() is { } moduleInfo ? GetRules(moduleInfo.FromEntity(), loadout) : AsyncEnumerable.Empty<ISortRule<Mod.Model, ModId>>();
    }

    // From what I guess, we will need to re-sort either when a mod was added/removed or a mod version changed
    // We could only consider the mods that are relevant for the self mod, but not sure if this will work correct
    // Investigate once testing is available
    public Hash GetFingerprint(ModId self, Loadout.Model loadout)
    {
        throw new NotImplementedException();
        /*
        var moduleInfos = loadout.Mods.Select(x => x.Value.GetModuleInfo()).OfType<ModuleInfoExtended>().OrderBy(x => x.Id).ToArray();

        using var fp = Fingerprinter.Create();
        fp.Add(loadout[self].ModId);
        foreach (var moduleInfo in moduleInfos)
        {
            fp.Add(moduleInfo.Id);
            fp.Add(moduleInfo.Version.ToString());
        }
        return fp.Digest();
        */
    }
}
