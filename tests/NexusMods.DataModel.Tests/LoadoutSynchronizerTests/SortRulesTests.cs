using System.Collections.Immutable;
using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Sorting.Rules;
using NexusMods.DataModel.Tests.Harness;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class SortRulesTests : ALoadoutSynrchonizerTest<SortRulesTests>
{
    public SortRulesTests(IServiceProvider provider) : base(provider)
    {

    }
    
    [Fact]
    public async Task GeneratedSortRulesAreFetched()
    {
       var loadout = await CreateTestLoadout(4);

       var mod = loadout.Value.Mods.Values.First(m => m.Name == "Mod 2");

       var nameForId = loadout.Value.Mods.Values.ToDictionary(m => m.Id, m => m.Name);

       var rules = await LoadoutSynchronizer.ModSortRules(loadout.Value, mod).ToListAsync();


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

        var mods = await LoadoutSynchronizer.SortMods(loadout.Value);
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

        await LoadoutSynchronizer.Invoking(_ => LoadoutSynchronizer.SortMods(loadout.Value))
            .Should().ThrowAsync<InvalidOperationException>("rule conflicts with generated rules");
    }

    [Fact]
    public async Task SortRulesAreCached()
    {
        var loadout = await CreateTestLoadout(2);

        await TestSyncronizer.SortMods(loadout.Value);

        TestFingerprintCacheInstance.Dict.Should().HaveCount(2);

        TestFingerprintCacheInstance.GetCount.Values.Should().AllBeEquivalentTo(1);
        TestFingerprintCacheInstance.SetCount.Values.Should().AllBeEquivalentTo(1);
        
        await TestSyncronizer.SortMods(loadout.Value);
        
        TestFingerprintCacheInstance.GetCount.Values.Should().AllBeEquivalentTo(2);
        TestFingerprintCacheInstance.SetCount.Values.Should().AllBeEquivalentTo(1);

    }

   

}
