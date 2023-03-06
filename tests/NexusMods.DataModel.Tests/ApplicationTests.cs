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
        var mainList = await LoadoutManager.ManageGame(Install, "MainList", CancellationToken.None);
        await mainList.Install(DATA_ZIP_LZMA, "First Mod", CancellationToken.None);

        var plan = await mainList.MakeApplyPlan();
        plan.Steps.OfType<CopyFile>().Count().Should().Be(3);

        await mainList.Apply(plan, CancellationToken.None);

        var newPlan = await mainList.MakeApplyPlan();
        newPlan.Steps.Count.Should().Be(0);

        await BaseList?.Apply()!;
    }

    [Fact]
    public async Task CanIntegrateChanges()
    {
        var mainList = await LoadoutManager.ManageGame(Install, "MainList", Token);
        await mainList.Install(DATA_ZIP_LZMA, "First Mod", Token);
        await mainList.Install(DATA_7Z_LZMA2, "Second Mod", Token);

        var list = mainList.FlattenList().ToList();
        var originalPlan = await mainList.MakeApplyPlan();
        originalPlan.Steps.OfType<CopyFile>().Count().Should().Be(3, "Files override each other");

        await mainList.Apply(originalPlan, CancellationToken.None);

        var gameFolder = Install.Locations[GameFolderType.Game];
        foreach (var file in DATA_NAMES)
        {
            gameFolder.CombineUnchecked(file).FileExists.Should().BeTrue("File has been applied");
        }

        var fileToDelete = DATA_NAMES.First();
        var fileToModify = DATA_NAMES.Skip(1).First();

        gameFolder.CombineUnchecked(fileToDelete).Delete();
        await gameFolder.CombineUnchecked(fileToModify).WriteAllTextAsync("modified");
        var modifiedHash = "modified".XxHash64();

        var firstMod = mainList.Value.Mods.Values.First();
        var ingestPlan = await mainList.MakeIngestionPlan(_ => firstMod, Token).ToHashSet();

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

        await BaseList?.Apply(Token)!;
    }
}
