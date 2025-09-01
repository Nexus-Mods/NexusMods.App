using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.TestFramework;
using NexusMods.Hashing.xxHash3;
using NexusMods.HyperDuck;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.CreationEngine.Tests.SkyrimSE;

public class LibraryArchiveInstallerTests(ITestOutputHelper outputHelper) : AIsolatedGameTest<CollectionTests, CreationEngine.SkyrimSE.SkyrimSE>(outputHelper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddCreationEngine()
            .AddAdapters()
            .AddUniversalGameLocator<CreationEngine.SkyrimSE.SkyrimSE>(new Version("1.6.1"));
    }

    [Theory]
    // Has files in a named folder, which go in the game folder, but also includes a Data folder that contains files to be copied to the game folder.
    [InlineData("SKSE (Steam)", 30379, 462377)]
    // SKSE plugin has files all in a Data folder, this is considered a "normal" configuration where all files are in `/Data/...` 
    [InlineData("JContainers (SE)", 16495, 463765)]
    // contains meshes and textures for the Data folder, but data is spelled as `data` instead of `Data` so this is a case insensitive test
    [InlineData("Nordic Chair", 102400, 439688)]
    // Just contains a .swf file in an `Interface` folder
    [InlineData("Better Dialogue Controls", 1429, 11022)]
    // Just files in a meshes folder base
    [InlineData("Floating Ash Pile Fix", 63434, 264466)]
    public async Task CanInstallMod(string name, uint modId, uint fileId)
    {
        var loadout = await CreateLoadout();
        await using var tempFile = TemporaryFileManager.CreateFile();
        var download = await NexusModsLibrary.CreateDownloadJob(tempFile.Path, Game.GameId, ModId.From(modId), FileId.From(fileId));
        var libraryArchive = await LibraryService.AddDownload(download);

        var installed = await LibraryService.InstallItem(libraryArchive.AsLibraryItem(), loadout);

        var contents = installed.LoadoutItemGroup.Value.Children
            .OfTypeLoadoutItemWithTargetPath()
            .OfTypeLoadoutFile()
            .Select(child => ((GamePath)child.AsLoadoutItemWithTargetPath().TargetPath, child.Hash, child.Size))
            .OrderBy(x => x.Item1);

        await VerifyTable(contents, name);

    }
}
