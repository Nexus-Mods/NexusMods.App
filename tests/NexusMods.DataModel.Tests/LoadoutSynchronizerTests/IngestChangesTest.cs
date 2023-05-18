using FluentAssertions;
using NexusMods.DataModel.Loadouts;
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

    [Fact]
    public async Task RemovedFilesAreRemoved()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        var firstMod = loadout.Mods.Values.First();

        var absPath = GetFirstModFile(loadout);

        var newId = ModId.New();
        loadout = LoadoutManager.Registry.Alter(loadout.LoadoutId, "Dup Mod",
            loadout => loadout with
        {
            Mods = loadout.Mods.With(newId, firstMod with
            {
                Id = newId
            })
        });

        loadout.Mods.Count.Should().Be(2);

        (from mod in loadout.Mods.Values
            from file in mod.Files.Values
            select file).Count().Should().Be(2);

        loadout = await TestSyncronizer.Ingest(new IngestPlan
        {
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>(),
            Steps = new IIngestStep[]
            {
                new RemoveFromLoadout
                {
                    To = absPath
                }
            }
        });

        loadout.Mods.Count.Should().Be(2);

        (from mod in loadout.Mods.Values
            from file in mod.Files.Values
            select file).Count().Should().Be(0);

    }
}
