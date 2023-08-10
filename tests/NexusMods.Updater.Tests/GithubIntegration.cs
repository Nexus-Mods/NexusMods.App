using FluentAssertions;
using NexusMods.Updater.DownloadSources;
using NexusMods.Updater.Verbs;

namespace NexusMods.Updater.Tests;

public class GithubIntegration
{
    private readonly Github _github;
    private readonly ForceAppUpdate _forceAppUpdate;
    private readonly UpdaterService _updaterService;

    public GithubIntegration(Github github, ForceAppUpdate forceAppUpdate, UpdaterService updaterService)
    {
        _github = github;
        _forceAppUpdate = forceAppUpdate;
        _updaterService = updaterService;
    }

    [Fact]
    public async Task GetLatestRelease()
    {
        var latestRelease = await _github.GetLatestRelease("Nexus-Mods", "NexusMods.App");
        latestRelease.Should().NotBeNull();

        var version = Version.Parse(latestRelease!.Tag.TrimStart('v'));
        version.Should().BeGreaterThan(new Version(0, 0));
    }

    [Fact]
    public async Task CanForceDownload()
    {
        if (_updaterService.UpdateFolder.DirectoryExists())
            _updaterService.UpdateFolder.DeleteDirectory();

        await _forceAppUpdate.Run(CancellationToken.None);
        _updaterService.UpdateFolder.DirectoryExists().Should().BeTrue();

        _updaterService.UpdateFolder.Combine(Constants.UpdateExecutable).FileExists.Should().BeTrue();
        var updateFile = _updaterService.UpdateFolder.Combine(Constants.UpdateMarkerFile);
        updateFile.FileExists.Should().BeTrue();
        Version.Parse(await updateFile.ReadAllTextAsync()).Should().BeGreaterThan(new Version(0, 0, 0, 1));

        _updaterService.UpdateFolder.DeleteDirectory();
    }
}
