using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.DataModel.Loadouts.Mods;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Paths;
using NSubstitute;
using IGeneratedFile = NexusMods.DataModel.LoadoutSynchronizer.IGeneratedFile;

namespace NexusMods.DataModel.Tests.LoadoutSynchronizerTests;

public class ApplyLoadoutTests : ALoadoutSynrchonizerTest<ApplyLoadoutTests>
{
    public ApplyLoadoutTests(IServiceProvider provider) : base(provider) { }

    /// <summary>
    /// Verifies that files marked for backing up will be handed to the ArchiveManager
    /// for backing up
    /// </summary>
    [Fact]
    public async Task FilesMarkedForBackupAreBackedUp()
    {
        var loadout = await CreateApplyPlanTestLoadout();


        var file = GetFirstModFile(loadout);
        var plan = new IApplyStep[]
        {
            new BackupFile()
            {
                To = file,
                Hash = Hash.From(0x424),
                Size = Size.From(1024)
            }
        };

        TestArchiveManagerInstance.Archives.Should().NotContain(Hash.From(0x424));

        await TestSyncronizer.Apply(new ApplyPlan
        {
            Steps = plan,
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>()
        });

        TestArchiveManagerInstance.Archives.Should().Contain(Hash.From(0x424));
    }

    [Fact]
    public async Task FilesMarkedForDeletionAreDeleted()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var file = GetFirstModFile(loadout);

        var counter = 0;
        var fileSystem = Substitute.For<IFileSystem>();
        var file1 = file;
        fileSystem.When(x => x.DeleteFile(file1)).Do(_ => counter++);

        file = file.WithFileSystem(fileSystem);
        var plan = new IApplyStep[]
        {
            new DeleteFile
            {
                To = file,
                Hash = Hash.From(0x424),
                Size = Size.From(0x42)
            }
        };

        await TestSyncronizer.Apply(new ApplyPlan
        {
            Steps = plan,
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>()
        });

        counter.Should().Be(1);
    }

    [Fact]
    public async Task ExtractedFilesAreExtracted()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var file = GetFirstModFile(loadout);

        var plan = new IApplyStep[]
        {
            new ExtractFile
            {
                To = file,
                Hash = Hash.From(0x424),
                Size = Size.From(0x42)
            }
        };

        await TestSyncronizer.Apply(new ApplyPlan
        {
            Steps = plan,
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>()
        });

        TestArchiveManagerInstance.Extracted.Should().ContainKey(Hash.From(0x424));
    }


    class TestGeneratedFile : ITriggerFilter<ModFilePair, Plan>, Loadouts.ModFiles.IGeneratedFile
    {
        public ITriggerFilter<ModFilePair, Plan> TriggerFilter => this;
        public async Task<Hash> GenerateAsync(Stream stream, ApplyPlan plan, CancellationToken cancellationToken = default)
        {
            var txt = "Hello World!"u8.ToArray();
            await stream.WriteAsync(txt, cancellationToken);
            return txt.AsSpan().XxHash64();
        }

        public Hash GetFingerprint(ModFilePair self, Plan input)
        {
            return Hash.From(0xDEADBEEF);
        }
    }
    [Fact]
    public async Task GeneratedFilesAreGenerated()
    {
        var loadout = await CreateApplyPlanTestLoadout(true);

        var dest = loadout.Installation.LocationsRegister[LocationId.Game].Combine("generated.txt");
        var gFile = new TestGeneratedFile();


        var plan = new IApplyStep[]
        {
            new GenerateFile
            {
                To = dest,
                Fingerprint = Hash.From(0xDEADBEEF),
                Source = gFile
            }
        };

        await TestSyncronizer.Apply(new ApplyPlan
        {
            Steps = plan,
            Loadout = loadout,
            Mods = Array.Empty<Mod>(),
            Flattened = new Dictionary<GamePath, ModFilePair>()
        });

        dest.FileExists.Should().BeTrue();
        (await dest.ReadAllTextAsync()).Should().Be("Hello World!");

        TestGeneratedFileFingerprintCache.Dict.Should().Contain(KeyValuePair.Create(Hash.From(0xDEADBEEF),
            new CachedGeneratedFileData
            {
                DataStoreId = new Id64(EntityCategory.Fingerprints, 0xDEADBEEF),
                Hash = "Hello World!"u8.XxHash64(),
                Size = dest.FileInfo.Size
            }));
    }
}
