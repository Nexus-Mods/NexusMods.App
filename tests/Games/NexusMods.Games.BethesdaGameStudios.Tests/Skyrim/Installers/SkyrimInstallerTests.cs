using FluentAssertions;
using NexusMods.DataModel.Loadouts.ModFiles;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests.Skyrim.Installers;

/// <summary>
/// Tests for the Skyrim installer.
/// </summary>
public class SkyrimInstallerTests : AGameTest<SkyrimSpecialEdition>
{
    private readonly TestModDownloader _downloader;
    private readonly IFileSystem _realFs;

    public SkyrimInstallerTests(IServiceProvider serviceProvider, TestModDownloader downloader, IFileSystem realFs) : base(serviceProvider)
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

    [Fact]
    public async Task InstallMod_WithBsa() => await TestLooseFileCommon("HasBsa");

    [Fact]
    public async Task InstallMod_WithBsaInSubfolder() => await TestLooseFileCommon("HasBsa_InSubfolder");

    [Fact]
    public async Task InstallMod_WithEsl() => await TestLooseFileCommon("HasEsl");

    [Fact]
    public async Task InstallMod_WithEslInSubfolder() => await TestLooseFileCommon("HasEsl_InSubfolder");

    [Fact]
    public async Task InstallMod_WithEsm() => await TestLooseFileCommon("HasEsm");

    [Fact]
    public async Task InstallMod_WithEsmInSubfolder() => await TestLooseFileCommon("HasEsm_InSubfolder");

    [Fact]
    public async Task InstallMod_WithEsp() => await TestLooseFileCommon("HasEsp");

    [Fact]
    public async Task InstallMod_WithEspInSubfolder() => await TestLooseFileCommon("HasEsp_InSubfolder");

    private async Task TestLooseFileCommon(string folderName)
    {
        // TODO: Technically these tests don't cover where the file is sourced from, some code could be added here to do this.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var path = BethesdaTestHelpers.GetDownloadableModFolder(_realFs, folderName);
        var downloaded = await _downloader.DownloadFromManifestAsync(path, _realFs);

        var mod = await InstallModFromArchiveIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        var files = mod.Files;
        files.Count.Should().BeGreaterThan(0);
        
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
