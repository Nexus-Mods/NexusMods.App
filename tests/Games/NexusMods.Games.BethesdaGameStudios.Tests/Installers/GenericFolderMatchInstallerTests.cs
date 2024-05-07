﻿using FluentAssertions;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using static Xunit.Assert;

namespace NexusMods.Games.BethesdaGameStudios.Tests.Installers;

/// <summary>
/// Tests for the Skyrim installer.
/// </summary>
public abstract class GenericFolderMatchInstallerTests<TGame> : AGameTest<TGame> where TGame : AGame
{
    private readonly TestModDownloader _downloader;
    private readonly IFileSystem _realFs;

    protected GenericFolderMatchInstallerTests(IServiceProvider serviceProvider, TestModDownloader downloader,
        IFileSystem realFs) : base(serviceProvider)
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

    [Fact]
    public async Task InstallMod_WithScriptExtender()
    {
        var loadout = await CreateLoadout(indexGameFiles: false);
        var path = BethesdaTestHelpers.GetDownloadableModFolder(_realFs, "HasScriptExtender");
        var downloaded = await _downloader.DownloadFromManifestAsync(path, _realFs);

        var mod = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        var files = mod.Files;
        files.Count.Should().BeGreaterThan(0);
        files.Where(f => f.To.Path.Equals("skse_loader.exe")).Should()
            .HaveCount(1);
        files.Where(f => f.To.Path.StartsWith("Data/scripts")).Should()
            .HaveCount(120);
        files.Where(f => f.To.Path.StartsWith("src")).Should()
            .HaveCount(0);
    }

    protected async Task TestLooseFileCommon(string folderName)
    {
        // TODO: Technically these tests don't cover where the file is sourced from, some code could be added here to do this.
        var loadout = await CreateLoadout(indexGameFiles: false);
        var path = BethesdaTestHelpers.GetDownloadableModFolder(_realFs, folderName);
        var downloaded = await _downloader.DownloadFromManifestAsync(path, _realFs);

        var mod = await InstallModStoredFileIntoLoadout(
            loadout,
            downloaded.Path,
            downloaded.Manifest.Name);

        var files = mod.Files;
        files.Count.Should().BeGreaterThan(0);

        foreach (var file in files)
        {
            if (file.TryGetAsStoredFile(out var storedFile))
            {
                if (!file.To.Path.StartsWith("Data"))
                    Fail("Loose files should target data folder.");
                continue;
            }

            Fail("File should be recognised as from archive.");
        }
    }
}
