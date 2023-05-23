using FluentAssertions;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.DataModel.Tests;

public class ToolTests : ADataModelTest<ToolTests>
{
    public ToolTests(IServiceProvider provider) : base(provider)
    {
    }
    
    [Fact]
    public async Task CanRunTools()
    {
        throw new NotImplementedException();
        /*
        var name = Guid.NewGuid().ToString();
        var loadout = await LoadoutManager.ManageGameAsync(Install, name);
        await loadout.InstallModsFromArchiveAsync(Data7ZLzma2, "Mod1", CancellationToken.None);
        var gameFolder = loadout.Value.Installation.Locations[GameFolderType.Game];

        gameFolder.CombineUnchecked("files.txt").FileExists.Should().BeFalse("tool should not have run yet");
        gameFolder.CombineUnchecked("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");

        var tool = loadout.Tools.OfType<ListFilesTool>().First();
        await loadout.Run(tool, CancellationToken.None);

        gameFolder.CombineUnchecked("files.txt").FileExists.Should().BeTrue("tool should have run");
        gameFolder.CombineUnchecked("rootFile.txt").FileExists.Should().BeTrue("loadout has been automatically applied");

        var generatedFile = loadout.Value.Mods.Values
            .SelectMany(m => m.Files.Values)
            .FirstOrDefault(f => f.To == ListFilesTool.GeneratedFilePath);

        generatedFile.Should().NotBeNull("the generated file should be in the loadout");
        loadout.Value.Mods.Values.Where(m => m.Name == "List Files Generated Files")
            .Should().HaveCount(1, "the generated file should be in a generated mod");
*/
    }
}
