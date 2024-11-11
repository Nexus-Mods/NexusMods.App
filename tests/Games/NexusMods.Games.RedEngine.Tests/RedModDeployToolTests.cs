using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.Cyberpunk2077.LoadOrder;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.Games.RedEngine.Tests;

public class RedModDeployToolTests : ACyberpunkIsolatedGameTest<Cyberpunk2077Game>
{
    private readonly RedModDeployTool _tool;

    public RedModDeployToolTests(ITestOutputHelper helper) : base(helper)
    {
        _tool = ServiceProvider.GetServices<ITool>().OfType<RedModDeployTool>().Single();
    }
    
    [Fact]
    public async Task LoadorderFileIsWrittenCorrectly()
    {
        var loadout = await SetupLoadout();
        await Verify(await WriteLoadoutFile(loadout));
    }
    
    [Theory]
    [InlineData("Driver_Shotguns", 3)]
    [InlineData("Driver_Shotguns", -3)]
    [InlineData("Driver_Shotguns", -11)]
    [InlineData("maestros_of_synth_body_heat_radio", -1)]
    [InlineData("maestros_of_synth_body_heat_radio", 10)]
    [InlineData("maestros_of_synth_the_dirge", -11)]
    public async Task MovingModsRelativelyResultsInCorrectOrdering(string name, int delta)
    {
        var loadout = await SetupLoadout();
        
        var factory = ServiceProvider.GetRequiredService<RedModSortableItemProviderFactory>();
        var provider = factory.GetLoadoutSortableItemProvider(loadout);
        var order = provider.SortableItems;
        var specificGroup = order.OfType<RedModSortableItem>().Single(g => g.DisplayName == name);
        
        await provider.SetRelativePosition(specificGroup, delta);
        
        loadout = loadout.Rebase();
        await Verify(await WriteLoadoutFile(loadout)).UseParameters(name, delta);
    }


    private async Task<string> WriteLoadoutFile(Loadout.ReadOnly loadout)
    {
        await using var tempFile = TemporaryFileManager.CreateFile();
        loadout = loadout.Rebase();
        await _tool.WriteLoadorderFile(tempFile.Path, loadout);
        return await tempFile.Path.ReadAllTextAsync();
    }

    private async Task<Loadout.ReadOnly> SetupLoadout()
    {
        var loadout = await CreateLoadout();
        var files = new[] { "one_mod.7z", "several_red_mods.7z" };
        
        foreach (var file in files)
        {
            var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("LibraryArchiveInstallerTests/Resources/" + file);
            var libraryArchive = await RegisterLocalArchive(fullPath);
            await LibraryService.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout);
        }
        
        loadout = loadout.Rebase();
        return loadout;
    }


}
