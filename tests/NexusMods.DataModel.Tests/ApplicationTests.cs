using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Loadouts.ApplySteps;
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
        await mainList.InstallModAsync(DataZipLzma, "First Mod", CancellationToken.None);

        var plan = await mainList.MakeApplyPlanAsync();
        plan.Steps.OfType<CopyFile>().Count().Should().Be(3);

        await mainList.ApplyAsync(plan, CancellationToken.None);

        var newPlan = await mainList.MakeApplyPlanAsync();
        newPlan.Steps.Count.Should().Be(0);

        await BaseList.ApplyAsync();
    }

    [Fact]
    public async Task CanIntegrateChanges()
    {
        var mainList = await LoadoutManager.ManageGameAsync(Install, "MainList", Token);
        await mainList.InstallModAsync(DataZipLzma, "First Mod", Token);
        await mainList.InstallModAsync(Data7ZLzma2, "Second Mod", Token);

        var originalPlan = await mainList.MakeApplyPlanAsync();
        originalPlan.Steps.OfType<CopyFile>().Count().Should().Be(3, "Files override each other");

        await mainList.ApplyAsync(originalPlan, CancellationToken.None);

        var gameFolder = Install.Locations[GameFolderType.Game];
        foreach (var file in DataNames)
        {
            gameFolder.CombineUnchecked(file).FileExists.Should().BeTrue("File has been applied");
        }

        var fileToDelete = DataNames.First();
        var fileToModify = DataNames.Skip(1).First();

        gameFolder.CombineUnchecked(fileToDelete).Delete();
        await gameFolder.CombineUnchecked(fileToModify).WriteAllTextAsync("modified");
        var modifiedHash = "modified".XxHash64();

        var firstMod = mainList.Value.Mods.Values.First();
        var ingestPlan = await mainList.MakeIngestionPlanAsync(_ => firstMod, Token).ToHashSetAsync();

        ingestPlan.Should().BeEquivalentTo(new IApplyStep[]
        {
            new BackupFile()
            {
                To = gameFolder.CombineUnchecked(fileToModify),
                Size = Size.From("modified".Length),
                Hash = modifiedHash
            },
            new RemoveFromLoadout
            {
                To = gameFolder.CombineUnchecked(fileToDelete),
            },
            new IntegrateFile
            {
                To = gameFolder.CombineUnchecked(fileToModify),
                Size = Size.From("modified".Length),
                Hash = modifiedHash,
                Mod = firstMod
            }
        });


        mainList.FlattenList().Count().Should().Be(7, "because no changes are applied yet");

        await mainList.ApplyIngest(ingestPlan, Token);


        var flattened = mainList.FlattenList().ToDictionary(f => f.File.To);
        flattened.Count.Should().Be(6, "Because we've deleted one file");

        mainList.Value.Mods.Values
            .SelectMany(m => m.Files.Values)
            .OfType<AStaticModFile>()
            .Where(f => f.Hash == modifiedHash)
            .Should()
            .NotBeEmpty("Because we've updated a file");

        await BaseList.ApplyAsync(Token);
    }
}
