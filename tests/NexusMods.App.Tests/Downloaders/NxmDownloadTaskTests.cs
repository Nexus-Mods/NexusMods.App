using FluentAssertions;
using NexusMods.App.Downloaders;
using NexusMods.Games.RedEngine;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.App.Tests.Downloaders;

[Trait("RequiresNetworking", "True")]
public class NxmDownloadTaskTests : AGameTest<Cyberpunk2077>
{
    private readonly DownloadService _downloadService;

    public NxmDownloadTaskTests(IServiceProvider serviceProvider, DownloadService downloadService) : base(serviceProvider)
    {
        _downloadService = downloadService;
    }

    [Theory]
    [InlineData("cyberpunk2077", 107, 33156)]
    public async Task DownloadModFromNxm(string gameDomain, ulong modId, ulong fileId)
    {
        // This test requires Premium. If it fails w/o Premium, ignore that.
        var loadout = await CreateLoadout();
        var task = new NxmDownloadTask(LoadoutRegistry, TemporaryFileManager, NexusClient, HttpDownloader, ArchiveAnalyzer, ArchiveInstaller, _downloadService);
        var origNumMods = loadout.Value.Mods.Count;
        origNumMods.Should().Be(1); // game files

        var uri = $"nxm://{gameDomain}/mods/{modId}/files/{fileId}";
        task.Init(NXMUrl.Parse(uri));
        await task.StartAsync();
        loadout.Value.Mods.Count.Should().BeGreaterThan(origNumMods);
    }
}
