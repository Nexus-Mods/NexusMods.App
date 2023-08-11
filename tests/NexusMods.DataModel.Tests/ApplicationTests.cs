using FluentAssertions;
using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.DataModel.Loadouts.IngestSteps;
using NexusMods.DataModel.Loadouts.ModFiles;
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
        var mainList = await LoadoutManager.ManageGameAsync(Install, "MainList", CancellationToken.None);
        var hash = await ArchiveAnalyzer.AnalyzeFileAsync(DataZipLzma, CancellationToken.None);
        await ArchiveInstaller.AddMods(mainList.Value.LoadoutId, hash.Hash, "First Mod", CancellationToken.None);

        var plan = await LoadoutSynchronizer.MakeApplySteps(mainList.Value, CancellationToken.None);
        plan.Steps.OfType<ExtractFile>().Count().Should().Be(3);

        await LoadoutSynchronizer.Apply(plan, CancellationToken.None);
        
        var gameFolder = Install.Locations[GameFolderType.Game];
        foreach (var file in DataNames)
        {
            gameFolder.Combine(file).FileExists.Should().BeTrue("File has been applied");
        }

        var newPlan = await LoadoutSynchronizer.MakeApplySteps(mainList.Value, CancellationToken.None);
        newPlan.Steps.Count().Should().Be(0);
    }

    [Fact]
    public async Task CanIntegrateChanges()
    {
        var mainList = BaseList;
        await AddMods(mainList, DataZipLzma, "First Mod");
        await AddMods(mainList, Data7ZLzma2, "Second Mod");


        var originalPlan = await LoadoutSynchronizer.MakeApplySteps(mainList.Value, Token);
        originalPlan.Steps.OfType<ExtractFile>().Count().Should().Be(3, "Files override each other");
        
        await LoadoutSynchronizer.Apply(originalPlan, Token);
        
        var gameFolder = Install.Locations[GameFolderType.Game];
        foreach (var file in DataNames)
        {
            gameFolder.Combine(file).FileExists.Should().BeTrue("File has been applied");
        }

        var fileToDelete = DataNames.First();
        var fileToModify = DataNames.Skip(1).First();

        gameFolder.Combine(fileToDelete).Delete();
        await gameFolder.Combine(fileToModify).WriteAllTextAsync("modified");
        var modifiedHash = "modified".XxHash64AsUtf8();

        var firstMod = mainList.Value.Mods.Values.First();
        var ingestPlan = await LoadoutSynchronizer.MakeIngestPlan(mainList.Value, _ => firstMod.Id, CancellationToken.None); 
            
        ingestPlan.Steps.Should().BeEquivalentTo(new IIngestStep[]
        {
            new Loadouts.IngestSteps.BackupFile
            {
                Source = gameFolder.Combine(fileToModify),
                Size = Size.FromLong("modified".Length),
                Hash = modifiedHash
            },
            new Loadouts.IngestSteps.RemoveFromLoadout
            {
                Source = gameFolder.Combine(fileToDelete),
            },
            new CreateInLoadout
            {
                Source = gameFolder.Combine(fileToModify),
                Size = Size.FromLong("modified".Length),
                Hash = modifiedHash,
                ModId = firstMod.Id
            }
        });


        (await LoadoutSynchronizer.FlattenLoadout(mainList.Value)).Files.Count.Should().Be(7, "because no changes are applied yet");

        await LoadoutSynchronizer.Ingest(ingestPlan);
        
        var flattened = (await LoadoutSynchronizer.FlattenLoadout(mainList.Value)).Files.Count;
        flattened.Should().Be(6, "Because we've deleted one file");

        mainList.Value.Mods.Values
            .SelectMany(m => m.Files.Values)
            .OfType<IToFile>()
            .OfType<IFromArchive>()
            .Where(f => f.Hash == modifiedHash)
            .Should()
            .NotBeEmpty("Because we've updated a file");

    }

}
