using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda.Plugins;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Games.CreationEngine.Abstractions;
using NexusMods.Games.CreationEngine.Tests.TestAttributes;
using NexusMods.Games.IntegrationTestFramework;
using NexusMods.Paths;
using NexusMods.Telemetry;
using Xunit;

namespace NexusMods.Games.CreationEngine.Tests.SkyrimSE;

[SkyrimSESteamCurrent]
public class LibraryArchiveInstallerTests(Type gameType, GameLocatorResult locatorResult) : AGameIntegrationTest(gameType, locatorResult)
{
    [Test]
    // Has files in a named folder, which go in the game folder, but also includes a Data folder that contains files to be copied to the game folder.
    [Arguments("SKSE (Steam)", 30379, 462377)]
    // SKSE plugin has files all in a Data folder, this is considered a "normal" configuration where all files are in `/Data/...` 
    [Arguments("JContainers (SE)", 16495, 463765)]
    // contains meshes and textures for the Data folder, but data is spelled as `data` instead of `Data` so this is a case insensitive test
    [Arguments("Nordic Chair", 102400, 439688)]
    // Just contains a .swf file in an `Interface` folder
    [Arguments("Better Dialogue Controls", 1429, 11022)]
    // Just files in a meshes folder base
    [Arguments("Floating Ash Pile Fix", 63434, 264466)]
    // Files in a sounds folder base
    [Arguments("Misc Voice Consistency Fixes", 135051, 568516)]
    // Script/Source folders
    [Arguments("Magic Student (WIChangeLocation04) Quest Fix", 80676, 340731)]
    // An archive that contains a file swapper "_SWAP.ini" file
    [Arguments("Lightened Skyrim - Base Object Swapper edition", 111475, 652709)]
    // Raw BSA/ESP files in an archive (no subfolders)
    [Arguments("Vanilla Purity Patch", 157236, 659486)]
    // A SKSE folder in the root
    [Arguments("Exit Sneak On Sprint", 138669, 584283)]
    // Description Framework Configs
    [Arguments("Creation Club Item Descriptions", 106048, 598464)]
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

        await Verify(Table(contents), "md")
            .UseDirectory("Verification Files")
            .UseParameters(name);
    }

    [Test]
    [Arguments("Honeystrand Grove is an Actual Grove", 158433, 662320)]
    [Arguments("JK's Interiors Patch Collection", 35910, 549166)]
    [Trait("RequiresNetworking", "True")]
    public async Task CanReadPluginData(string name, uint modId, uint fileId)
    {
        var extensions = new[] { KnownCEExtensions.ESM, KnownCEExtensions.ESP, KnownCEExtensions.ESL };
        var loadout = await CreateLoadout();
        await using var tempFile = TemporaryFileManager.CreateFile();
        var download = await NexusModsLibrary.CreateDownloadJob(tempFile.Path, Game.GameId, ModId.From(modId), FileId.From(fileId));
        var libraryArchive = await LibraryService.AddDownload(download);

        var installed = await LibraryService.InstallItem(libraryArchive.AsLibraryItem(), loadout);
        
        var plugins = installed.LoadoutItemGroup!.Value.Children
            .OfTypeLoadoutItemWithTargetPath()
            .Where(t => extensions.Contains(t.TargetPath.Item3.Extension))
            .OfTypeLoadoutFile()
            .ToArray();


        List<(string Plugin, int Index, string Master, ModType Type)> data = [];
        foreach (var plugin in plugins)
        {
            var fileName = plugin.AsLoadoutItemWithTargetPath().TargetPath.Item3.FileName;
            var header = await ((ICreationEngineGame) Game).ParsePlugin(plugin.Hash, fileName);
            var index = 0;
            foreach (var masterRef in header!.MasterReferences)
            {
                data.Add((fileName, index, masterRef.Master.FileName.ToString(), masterRef.Master.Type));
                index++;
            }
        }

        await Verify(Table(data))
            .UseDirectory("Verification Files")
            .UseParameters(name);
    }
}
