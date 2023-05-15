using System.Collections.Immutable;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.DataModel.TriggerFilter;

namespace NexusMods.DataModel.Tests;

public class LoadoutSyncronizerTests : ADataModelTest<LoadoutSyncronizerTests>
{
    public LoadoutSyncronizerTests(IServiceProvider provider) : base(provider)
    {
        
    }

    
    [JsonName("TestGeneratedSortRule")]
    public class GeneratedSortRule : IGeneratedSortRule, ISortRule<Mod, ModId>
    {
        public ITriggerFilter<ModId, Loadout> TriggerFilter { get; }
        
        public async IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId selfId, Loadout loadout)
        {
            foreach (var (modId, _) in loadout.Mods)
            {
                if (modId.Equals(selfId)) continue;
                yield return new After<Mod, ModId>(modId);
            }
        }
    }
    
    [Fact]
    public async Task GeneratedSortRulesAreFetched()
    { 
       var loadout = await LoadoutManager.ManageGameAsync(Install, Guid.NewGuid().ToString());
       var mod = new Mod()
       {
           Id = ModId.New(),
           Name = "Test Mod",
           Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
           SortRules = new ISortRule<Mod, ModId>[]
           {
               new GeneratedSortRule()
           }.ToImmutableList()
       };
       
       loadout.Add(mod);

       var rules = await LoadoutSyncronizer.ModSortRules(loadout.Value, mod).ToListAsync();

       rules.Should().HaveCountGreaterThanOrEqualTo(1, "a rule was generated");
       rules.Should().AllBeAssignableTo<After<Mod, ModId>>("generated rules are After rules");
       rules.Select(r => ((After<Mod, ModId>)r).Other).Should().NotContain(mod.Id, "because generated rules are not self-referential");
    }

}
