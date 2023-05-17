using FluentAssertions;
using Moq;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

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

        await TestSyncronizer.Apply(plan);

        TestArchiveManagerInstance.Archives.Should().Contain(Hash.From(0x424));
    }

    [Fact]
    public async Task FilesMarkedForDeletionAreDeleted()
    {
        var loadout = await CreateApplyPlanTestLoadout();

        var file = GetFirstModFile(loadout);

        var mfilesystem = new Mock<IFileSystem>();
        mfilesystem.Setup(f => f.DeleteFile(file));



        file = file.WithFileSystem(mfilesystem.Object);
        var plan = new IApplyStep[]
        {
            new DeleteFile()
            {
                To = file,
                Hash = Hash.From(0x424),
                Size = Size.From(0x42)
            }
        };

        await TestSyncronizer.Apply(plan);

        mfilesystem.Verify(f => f.DeleteFile(file), Times.Once);


    }
}
