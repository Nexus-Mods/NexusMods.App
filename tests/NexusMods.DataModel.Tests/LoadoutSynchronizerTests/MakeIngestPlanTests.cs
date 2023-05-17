using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.IngestSteps;
using NexusMods.DataModel.Loadouts.ModFiles;
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

    
    [Fact]
    public async Task ChangedFilesAreUpdatedInTheLoadoutAndBackedUp()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat");

        var fileOne = (from mod in loadout.Mods
            from file in mod.Value.Files
            where file.Value is IFromArchive
            select (mod.Key, file.Key, file.Value)).First();

        TestIndexer.Entries.Add(new HashedEntry(absPath, Hash.From(0x4242), DateTime.Now - TimeSpan.FromMinutes(20), Size.From(10)));

        
        var plan = await TestSyncronizer.MakeIngestPlan(loadout).ToListAsync();
        
        
        plan.Should().Contain(new BackupFile
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10)
        });

        plan.Should().Contain(new ReplaceInLoadout
        {
            To = absPath,
            Hash = Hash.From(0x4242),
            Size = Size.From(10),
            ModId = fileOne.Item1,
            ModFileId = fileOne.Item2,
        });
        
    }
    
    [Fact]
    public async Task DeletedFilesAreRemovedFromTheLoadout()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat");
        
        var plan = await TestSyncronizer.MakeIngestPlan(loadout).ToListAsync();
        
        plan.Should().Contain(new RemoveFromLoadout
        {
            To = absPath
        });

    }
}
