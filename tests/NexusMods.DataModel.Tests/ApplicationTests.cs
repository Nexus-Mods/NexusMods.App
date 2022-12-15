using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.ModLists;
using NexusMods.DataModel.ModLists.ApplySteps;
using NexusMods.DataModel.ModLists.Markers;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class ApplicationTests : ADataModelTest<ApplicationTests>
{
    
    public ApplicationTests(IServiceProvider provider) : base(provider)
    {
        
    }

    [Fact]
    public async Task CanApplyGame()
    {
        var mainList = await ModListManager.ManageGame(Install, "MainList", CancellationToken.None);
        await mainList.Install(DATA_ZIP_LZMA, "First Mod", CancellationToken.None);

        var plan = await mainList.MakeApplyPlan().ToList();
        plan.OfType<CopyFile>().Count().Should().Be(3);

        await mainList.ApplyPlan(plan, CancellationToken.None);

        var newPlan = await mainList.MakeApplyPlan().ToList();
        newPlan.Count.Should().Be(0);

        await BaseList.ApplyPlan(await BaseList.MakeApplyPlan().ToList());
    }
    
    [Fact]
    public async Task CanIntegrateChanges()
    {
        var mainList = await ModListManager.ManageGame(Install, "MainList", Token);
        await mainList.Install(DATA_ZIP_LZMA, "First Mod", Token);
        await mainList.Install(DATA_7Z_LZMA2, "Second Mod", Token);

        var originalPlan = await mainList.MakeApplyPlan().ToList();
        originalPlan.OfType<CopyFile>().Count().Should().Be(3, "Files override each other");

        await mainList.ApplyPlan(originalPlan, CancellationToken.None);

        var gameFolder = Install.Locations[GameFolderType.Game];
        foreach (var file in DATA_NAMES)
        {
            gameFolder.Join(file).FileExists.Should().BeTrue("File has been applied");
        }

        var fileToDelete = DATA_NAMES.First();
        var fileToModify = DATA_NAMES.Skip(1).First();
        
        gameFolder.Join(fileToDelete).Delete();
        await gameFolder.Join(fileToModify).WriteAllTextAsync("modified");
        var modifiedHash = "modified".XxHash64();

        var firstMod = mainList.Value.Mods.First();
        var ingestPlan = await mainList.MakeIngestionPlan(x => firstMod, Token).ToHashSet();

        ingestPlan.Should().BeEquivalentTo(new IApplyStep[]
        {
            new BackupFile()
            {
                To = fileToModify.RelativeTo(gameFolder),
                Size = "modified".Length,
                Hash = modifiedHash
            },
            new RemoveFromLoadout
            {
                To = fileToDelete.RelativeTo(gameFolder),
            },
            new IntegrateFile
            {
                To = fileToModify.RelativeTo(gameFolder),
                Size = "modified".Length,
                Hash = modifiedHash,
                Mod = firstMod
            }
        });

        await mainList.ApplyIngest(ingestPlan, Token);


        await BaseList.Apply(Token);
    }
}