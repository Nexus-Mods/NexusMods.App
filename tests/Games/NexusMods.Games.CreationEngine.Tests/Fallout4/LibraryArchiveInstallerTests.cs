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
    // DLL and INI file in the root folder
    [InlineData("xSE Plugin Preloader F4", 33946, 323314)]
    [InlineData("xSE Plugin Preloader F4 (older)", 33946, 221778)]
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
