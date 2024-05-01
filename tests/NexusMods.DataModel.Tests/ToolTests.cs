using EmptyFiles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.DataModel.LoadoutSynchronizer.Extensions;
using NexusMods.DataModel.Tests.Harness;
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
        await AddMods(BaseLoadout, Data7ZLzma2, "Mod1");
        var gameFolder = BaseLoadout.Installation.LocationsRegister[LocationId.Game];

        gameFolder.Combine("toolFiles.txt").FileExists.Should().BeFalse("tool should not have run yet");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");

        var tool = ToolManager.GetTools(BaseLoadout).OfType<ListFilesTool>().First();
        var result = await ToolManager.RunTool(tool, BaseLoadout);

        gameFolder.Combine("toolFiles.txt").FileExists.Should().BeTrue("tool should have run");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not been automatically applied");
        
        Refresh(ref BaseLoadout);
        var generatedFile = BaseLoadout.Files
            .FirstOrDefault(f => f.To == ListFilesTool.GeneratedFilePath);

        // Disabled until we rework generated files
        generatedFile.Should().NotBeNull("the generated file should be in the loadout");
        BaseLoadout.Mods.Where(m => m.Category == ModCategory.Overrides)
            .Should().HaveCount(1, "the generated file should be in a generated mod");
        
    }
    
}
