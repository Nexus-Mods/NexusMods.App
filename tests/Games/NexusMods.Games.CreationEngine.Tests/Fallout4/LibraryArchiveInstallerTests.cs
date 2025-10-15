using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.CreationEngine.Tests.TestAttributes;
using NexusMods.Games.IntegrationTestFramework;

namespace NexusMods.Games.CreationEngine.Tests.Fallout4;


[Fallout4SteamCurrent]
public class LibraryArchiveInstallerTests(Type gameType, GameLocatorResult locatorResult) : AGameIntegrationTest(gameType, locatorResult)
{
    [Test]
    // DLL and INI file in the root folder (winhttp.dll)
    [Arguments("xSE Plugin Preloader F4", 33946, 323314)]
    // DLL with a different name (iphlapi.dll)
    [Arguments("xSE Plugin Preloader F4 (older)", 33946, 221778)]
    // Has a fomod folder, but with no actual fomod installer script, just info.xml
    [Arguments("Shortcut to Curie", 31766, 129553)]
    // Has BA2/ESL files in a sub-folder named "zapgun"
    [Arguments("Zap Gun -ESL version", 53998, 217156)]
    // A tool installed to the Tools folder
    [Arguments("Collective Modding Toolkit", 87907, 344771)]
    // Files in a `strings` sub-folder
    [Arguments("A StoryWealth - Point Lookout", 60927, 243052)]
    // Folder named "Materials" in root
    [Arguments("Natural Landscapes - Invisible Dirt Fix", 71554, 278064)]
    // Data folder with top level license files
    [Arguments("Disk Cache Enabler", 74841, 290028)]
    // Preview .jpg files in root, ESL/ESPs in sub-folders
    [Arguments("Red Shift PA", 46589,188004)]
    // Just an archive with the exe in the root
    [Arguments("Fallout 4 Downgrader", 81630, 364228)]
    // Config folder at root, that puts files into Data/Config
    [Arguments("Customizable Character Rim Lighting Settings", 60927, 343009)]
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

        await Verify(Table(contents))
            .UseParameters(name)
            .UseDirectory("Verification Files");
    }
}
