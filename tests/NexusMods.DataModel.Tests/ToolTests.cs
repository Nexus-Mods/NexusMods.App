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
        await AddMods(BaseList, Data7ZLzma2, "Mod1");
        var gameFolder = BaseList.Value.Installation.LocationsRegister[LocationId.Game];

        gameFolder.Combine("toolFiles.txt").FileExists.Should().BeFalse("tool should not have run yet");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not yet been applied");

        var tool = ToolManager.GetTools(BaseList.Value).OfType<ListFilesTool>().First();
        var result = await ToolManager.RunTool(tool, BaseList.Value);

        gameFolder.Combine("toolFiles.txt").FileExists.Should().BeTrue("tool should have run");
        gameFolder.Combine("rootFile.txt").FileExists.Should().BeFalse("loadout has not been automatically applied");
        
        result.Should().NotBe(BaseList.Value, "the result should be a new loadout");
        
        var generatedFile = BaseList.Value.Mods.Values
            .SelectMany(m => m.Files.Values)
            .OfType<IToFile>()
            .FirstOrDefault(f => f.To == ListFilesTool.GeneratedFilePath);

        // Disabled until we rework generated files
        generatedFile.Should().NotBeNull("the generated file should be in the loadout");
        BaseList.Value.Mods.Values.Where(m => m.ModCategory == Mod.OverridesCategory)
            .Should().HaveCount(1, "the generated file should be in a generated mod");
        
        var latestLoadout = LoadoutRegistry.Get(result.LoadoutId);
        latestLoadout.Should().NotBeNull();
        latestLoadout!.DataStoreId.Should().BeEquivalentTo(result.DataStoreId, "the result should be the active loadout");
    }
}
