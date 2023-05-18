using FluentAssertions;
using NexusMods.DataModel.Loadouts.IngestSteps;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class IngestChangesTest : ALoadoutSynrchonizerTest<IngestChangesTest>
{
    public IngestChangesTest(IServiceProvider provider) : base(provider) { }

    [Fact]
    public async Task BackedUpFilesAreBackedUp()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        
        var absPath = loadout.Installation.Locations[GameFolderType.Game].CombineUnchecked("0x00001.dat");
        
        await TestSyncronizer.Ingest(new IngestPlan()
        {
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>(),
            Steps = new IIngestStep[]
            {
                new BackupFile()
                {
                    To = absPath,
                    Hash = Hash.From(0x1DEADBEEF),
                    Size = Size.From(0x2DEADBEEF)
                }
            }
        });

        TestArchiveManagerInstance.Archives.Should().Contain(Hash.From(0x1DEADBEEF));
    }
}
