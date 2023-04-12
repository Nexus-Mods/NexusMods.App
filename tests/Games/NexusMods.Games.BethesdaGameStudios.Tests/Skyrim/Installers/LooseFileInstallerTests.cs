using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;

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

    [Theory, AutoFileSystem]
    public async Task InstallMod_WithDataFolderAsync(InMemoryFileSystem fs)
    {
        var loadout = await LoadoutManager.ImportSkyrimSELoadoutAsync(_realFs);
        var path = BethesdaTestHelpers.GetDownloadableModFolder(_realFs, "RootedAtDataFolder");
        var files = await _downloader.DownloadFromManifestAsync(path, fs);

        var a = 5;
    }
}
