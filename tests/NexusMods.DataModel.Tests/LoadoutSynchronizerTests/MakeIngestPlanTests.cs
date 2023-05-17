using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class MakeIngestPlanTests : ALoadoutSynrchonizerTest<MakeIngestPlanTests>
{
    public MakeIngestPlanTests(IServiceProvider provider) : base(provider)
    {
    }


    /// <summary>
    /// New files in the game folder need to be backed up if they don't already exist in the archive manager.
    /// Either way they need to be added to the loadout. 
    /// </summary>
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

        plan.Should().ContainEquivalentOf(new CreateInLoadout
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10)
        }, opt => opt.RespectingRuntimeTypes());
    }
    
    /// <summary>
    /// New files in the game folder should not be backed up if they are already backed up.
    /// </summary>
    [Fact]
    public async Task FilesThatAreAlreadyBackedUpShouldNotBeBackedUpAgain()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.extra_file");
        
        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x4242), DateTime.Now - TimeSpan.FromMinutes(20), Size.From(10)));
        TestArchiveManagerInstance.Archives.Add(Hash.From(0x4242));
        
        var plan = await TestSyncronizer.MakeIngestPlan(loadout).ToListAsync();
        
        plan.Should().NotContainEquivalentOf(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10)
        });

        plan.Should().ContainEquivalentOf(new CreateInLoadout
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10)
        });
    }
}
