using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class MakeIngestPlanTests : ALoadoutSynrchonizerTest<MakeIngestPlanTests>
{
    public MakeIngestPlanTests(IServiceProvider provider) : base(provider) { }


    [Fact]
    public async Task FilesThatDontExistInLoadoutAreAddedAndBackedUp()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        

        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.extra_file");
        
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x4242), DateTime.Now - TimeSpan.FromMinutes(20), Size.From(10)));
        
        var plan = await TestSyncronizer.MakeIngestPlan(loadout).ToListAsync();
        
        plan.Should().ContainEquivalentOf(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10)
        });
    }
}
