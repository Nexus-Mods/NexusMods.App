using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.Skyrim.Installers;

/// <summary>
/// Tests for the loose file installer.
/// </summary>
public class LooseFileInstallerTests : AGameTest<SkyrimSpecialEdition>
{
    private readonly TestModDownloader _downloader;
    private readonly IFileSystem _realFs;

    public LooseFileInstallerTests(IServiceProvider serviceProvider, TestModDownloader downloader, IFileSystem realFs) : base(serviceProvider)
    {
        _downloader = downloader;
        _realFs = realFs;
    }

    // TODO: Use memory filesystem. We currently cannot do this because install code is not yet transitioned to IFileSystem; that said rest is a-ok!.

    [Fact]
    public async Task InstallMod_WithRootAtDataFolder() => await TestLooseFileCommon("RootedAtDataFolder");

    [Fact]
    public async Task InstallMod_WithRootAtGameFolder() => await TestLooseFileCommon("RootedAtGameFolder");

    [Fact]
    public async Task InstallMod_WithMisnamedDataFolder() => await TestLooseFileCommon("DataFolderWithDifferentName");

    private async Task TestLooseFileCommon(string folderName)
    {
        var loadout = await LoadoutManager.ImportSkyrimSELoadoutAsync(_realFs);
        var path = BethesdaTestHelpers.GetDownloadableModFolder(_realFs, folderName);
        var downloaded = await _downloader.DownloadFromManifestAsync(path, _realFs);

        var installedId = await loadout.InstallModAsync(downloaded.Path, downloaded.Manifest.Name);
        var files = loadout.Value.Mods[installedId].Files;
        foreach (var file in files)
        {
            if (file.Value is FromArchive fromArchive)
            {
                if (!fromArchive.To.Path.StartsWith("Data"))
                    Assert.Fail("Loose files should target data folder.");

                continue;
            }

            Assert.Fail("File should be recognised as from archive.");
        }
    }
}
