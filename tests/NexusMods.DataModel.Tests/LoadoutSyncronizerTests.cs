using System.Collections.Immutable;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.DataModel.TriggerFilter;
using static System.String;

namespace NexusMods.DataModel.Tests;

public class LoadoutSyncronizerTests : ADataModelTest<LoadoutSyncronizerTests>
{
    public LoadoutSyncronizerTests(IServiceProvider provider) : base(provider)
    {
        
    }

    
    
    
    
    [Fact]
    public async Task GeneratedSortRulesAreFetched()
    { 
       var loadout = await CreateTestLoadout(4);

       var mod = loadout.Value.Mods.Values.First(m => m.Name == "Mod 2");

       var nameForId = loadout.Value.Mods.Values.ToDictionary(m => m.Id, m => m.Name);
       
       var rules = await LoadoutSyncronizer.ModSortRules(loadout.Value, mod).ToListAsync();


       var testData = rules.Select(r =>
       {
           if (r is After<Mod, ModId> a) return ("After", nameForId[a.Other]);
           if (r is Before<Mod, ModId> b) return ("Before", nameForId[b.Other]);
           throw new NotImplementedException();
       });

       testData.Should().BeEquivalentTo(new[]
       {
           ("After", "Mod 0"),
           ("After", "Mod 1"),
           ("Before", "Mod 3")
       });
    }

    [Fact]
    public async Task CanSortMods()
    {
        var loadout = await CreateTestLoadout();

        var mods = await LoadoutSyncronizer.SortMods(loadout.Value);
        mods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "Mod 0",
            "Mod 1",
            "Mod 2",
            "Mod 3",
            "Mod 4",
            "Mod 5",
            "Mod 6",
            "Mod 7",
            "Mod 8",
            "Mod 9"
        }, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public async Task StaticRulesAreConsidered()
    {
        var lastMod = new Mod()
        {
            Id = ModId.New(),
            Name = "zz Last Mod",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
            SortRules = new ISortRule<Mod, ModId>[]
            {
                new First<Mod, ModId>()
            }.ToImmutableList()
        };
        
        var loadout = await CreateTestLoadout();
        loadout.Add(lastMod);
        
        await LoadoutSyncronizer.Invoking(_ => LoadoutSyncronizer.SortMods(loadout.Value))
            .Should().ThrowAsync<InvalidOperationException>("rule conflicts with generated rules");
    }
    
    
    
    /// <summary>
    /// Example generated sort rule that sorts all mods alphabetically
    /// </summary>
    [JsonName("TestGeneratedSortRule")]
    public class AlphabeticalSort : IGeneratedSortRule, ISortRule<Mod, ModId>
    {
        public ITriggerFilter<ModId, Loadout> TriggerFilter { get; }
        
        public async IAsyncEnumerable<ISortRule<Mod, ModId>> GenerateSortRules(ModId selfId, Loadout loadout)
        {
            var thisMod = loadout.Mods[selfId];
            foreach (var (modId, other) in loadout.Mods)
            {
                if (modId.Equals(selfId)) continue;
                if (Compare(other.Name, thisMod.Name, StringComparison.Ordinal) > 0)
                {
                    yield return new Before<Mod, ModId>(other.Id);
                }
                else
                {
                    yield return new After<Mod, ModId>(modId);
                }
            }
        }
    }

    
    /// <summary>
    /// Create a test loadout with a number of mods each with a alphabetical sort rule
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private async Task<LoadoutMarker> CreateTestLoadout(int number = 10)
    {
        var loadout = await LoadoutManager.ManageGameAsync(Install, Guid.NewGuid().ToString());

        var mainMod = loadout.Value.Mods.Values.First();
        
        var mods = Enumerable.Range(0,  number).Select(x => new Mod()
        {
            Id = ModId.New(),
            Name = $"Mod {x}",
            Files = EntityDictionary<ModFileId, AModFile>.Empty(DataStore),
            SortRules = new ISortRule<Mod, ModId>[]
            {
                new AlphabeticalSort()
            }.ToImmutableList()
        }).ToList();

        foreach (var mod in mods)
            loadout.Add(mod);
        
        loadout.Remove(mainMod);
        return loadout;
    }
}
