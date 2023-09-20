using FluentAssertions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.IngestSteps;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
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

        var absPath = loadout.Installation.LocationsRegister[GameFolderType.Game].Combine("0x00001.dat");

        await TestSyncronizer.Ingest(new IngestPlan()
        {
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>(),
            Steps = new IIngestStep[]
            {
                new BackupFile()
                {
                    Source = absPath,
                    Hash = Hash.From(0x1DEADBEEF),
                    Size = Size.From(0x2DEADBEEF)
                }
            }
        });

        TestArchiveManagerInstance.Archives.Should().Contain(Hash.From(0x1DEADBEEF));
    }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task RemovedFilesAreRemoved()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        var firstMod = loadout.Mods.Values.First(mod => mod.Enabled == true);

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

        loadout.Mods.Count.Should().Be(3);

        (from mod in loadout.Mods.Values
            where mod.Enabled == true
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
                    Source = absPath
                }
            }
        });

        loadout.Mods.Count.Should().Be(3);

        (from mod in loadout.Mods.Values
            where mod.Enabled == true
            from file in mod.Files.Values
            select file).Count().Should().Be(0);
    }

    [Fact]
    public async Task ChangedFilesAreChanged()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var firstMod = loadout.Mods.Values.First();
        var firstFile = firstMod.Files.Values.First();
        var absPath = loadout.Installation.LocationsRegister[GameFolderType.Game].Combine("foo.bar");

        loadout = await TestSyncronizer.Ingest(new IngestPlan
        {
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>(),
            Steps = new IIngestStep[]
            {
                new ReplaceInLoadout
                {
                    ModId = firstMod.Id,
                    ModFileId = firstFile.Id,
                    Hash = Hash.From(0x42DEADBEEF),
                    Size = Size.MB,
                    Source = absPath
                }
            }
        });

        var file = (FromArchive)loadout.Mods.Values.First().Files.Values.First();
        file.To.Should().Be(new GamePath(GameFolderType.Game, "foo.bar"));
        file.Hash.Should().Be(Hash.From(0x42DEADBEEF));
        file.Size.Should().Be(Size.MB);
    }

    [Fact]
    public async Task CreatedFilesAreAdded()
    {
        var loadout = await CreateApplyPlanTestLoadout();
        var firstMod = loadout.Mods.Values.First();
        var absPath = loadout.Installation.LocationsRegister[GameFolderType.Game].Combine("foo.bar");

        loadout = await TestSyncronizer.Ingest(new IngestPlan
        {
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>(),
            Steps = new IIngestStep[]
            {
                new CreateInLoadout
                {
                    ModId = firstMod.Id,
                    Hash = Hash.From(0x42DEADBEEF),
                    Size = Size.MB,
                    Source = absPath
                }
            }
        });

        var gamePath = new GamePath(GameFolderType.Game, "foo.bar");

        var file = loadout.Mods[firstMod.Id]
            .Files.Values
            .OfType<FromArchive>().First(f => f.To == gamePath);
        file.Hash.Should().Be(Hash.From(0x42DEADBEEF));
        file.Size.Should().Be(Size.MB);
    }
}
