using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using Xunit.Abstractions;

namespace NexusMods.Games.RedEngine.Tests;

public class RedModDeployToolTests(ITestOutputHelper helper) : ACyberpunkIsolatedGameTest<Cyberpunk2077Game>(helper)
{
    [Fact]
    public async Task LoadorderFileIsWrittenCorrectly()
    {
        var loadout = await CreateLoadout();
        var files = new[] { "one_mod.7z", "several_red_mods.7z" };
        
        foreach (var file in files)
        {
            var fullPath = FileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("LibraryArchiveInstallerTests/Resources/" + file);
            var libraryArchive = await RegisterLocalArchive(fullPath);
            await LibraryService.InstallItem(libraryArchive.AsLibraryFile().AsLibraryItem(), loadout);
        }

        await using var tempFile = TemporaryFileManager.CreateFile();
        var deployTool = ServiceProvider.GetServices<ITool>().OfType<RedModDeployTool>().Single();
        loadout = loadout.Rebase();
        await deployTool.WriteLoadorderFile(tempFile.Path, loadout);


        await Verify(await tempFile.Path.ReadAllTextAsync());

    }
    
}
