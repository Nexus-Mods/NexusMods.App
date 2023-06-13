using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
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
        var name = Guid.NewGuid().ToString();
        var loadout = await LoadoutManager.ManageGameAsync(Install, name);
        await AddMods(loadout, Data7ZLzma2, "Mod1");
        var gameFolder = loadout.Value.Installation.Locations[GameFolderType.Game];

        gameFolder.Combine("files.txt").FileExists.Should().BeFalse("tool should not have run yet");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");

        var tool = ToolManager.GetTools(loadout.Value).OfType<ListFilesTool>().First();
        await ToolManager.RunTool(tool, loadout.Value);
        
        gameFolder.Combine("files.txt").FileExists.Should().BeTrue("tool should have run");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeTrue("loadout has been automatically applied");

        var generatedFile = loadout.Value.Mods.Values
            .SelectMany(m => m.Files.Values)
            .OfType<IToFile>()
            .FirstOrDefault(f => f.To == ListFilesTool.GeneratedFilePath);

        generatedFile.Should().NotBeNull("the generated file should be in the loadout");
        loadout.Value.Mods.Values.Where(m => m.Name == "List Files Generated Files")
            .Should().HaveCount(1, "the generated file should be in a generated mod");
    }
}
