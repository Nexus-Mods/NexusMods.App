using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.TestFramework;
using NexusMods.HyperDuck;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.CreationEngine.Tests.Fallout4;

public class LibraryArchiveInstallerTests(ITestOutputHelper outputHelper) : AIsolatedGameTest<LibraryArchiveInstallerTests, CreationEngine.Fallout4.Fallout4>(outputHelper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddCreationEngine()
            .AddAdapters()
            .AddUniversalGameLocator<CreationEngine.Fallout4.Fallout4>(new Version("1.10.163"));
    }
    
    [Theory]
    // DLL and INI file in the root folder (winhttp.dll)
    [InlineData("xSE Plugin Preloader F4", 33946, 323314)]
    // DLL with a different name (iphlapi.dll)
    [InlineData("xSE Plugin Preloader F4 (older)", 33946, 221778)]
    // Has a fomod folder, but with no actual fomod installer script, just info.xml
    [InlineData("Shortcut to Curie", 31766, 129553)]
    // Has BA2/ESL files in a sub-folder named "zapgun"
    [InlineData("Zap Gun -ESL version", 53998, 217156)]
    // A tool installed to the Tools folder
    [InlineData("Collective Modding Toolkit", 87907, 344771)]
    // Files in a `strings` sub-folder
    [InlineData("A StoryWealth - Point Lookout", 60927, 243052)]
    // Folder named "Materials" in root
    [InlineData("Natural Landscapes - Invisible Dirt Fix", 71554, 278064)]
    // Data folder with top level license files
    [InlineData("Disk Cache Enabler", 74841, 290028)]
    // Preview .jpg files in root, ESL/ESPs in sub-folders
    [InlineData("Red Shift PA", 46589,188004)]
    // Just an archive with the exe in the root
    [InlineData("Fallout 4 Downgrader", 81630, 364228)]
    // Config folder at root, that puts files into Data/Config
    [InlineData("Customizable Character Rim Lighting Settings", 60927, 343009)]
    [Trait("RequiresNetworking", "True")]
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
