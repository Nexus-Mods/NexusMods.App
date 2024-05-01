using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Loadouts.Sorting;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Triggers;

namespace NexusMods.Games.StardewValley.Sorters;

[JsonName("NexusMods.Games.StardewValley.Sorters")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SMAPISorter : IGeneratedSortRule, ISortRule<Mod, ModId>
{
    public ITriggerFilter<ModId, Loadout> TriggerFilter => throw new NotImplementedException();

    public async IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId modId, Loadout loadout)
    {
        await Task.Yield();

        var enumerable = loadout.Mods
            .Where(mod => mod.Value.ModCategory == Mod.GameFilesCategory)
            .Select(mod => mod.Key)
            .Select(id => new After<Mod, ModId>
            {
                Other = id
            });

        foreach (var item in enumerable)
        {
            yield return item;
        }
    }
}
