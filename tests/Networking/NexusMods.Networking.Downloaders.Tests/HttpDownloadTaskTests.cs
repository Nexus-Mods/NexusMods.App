using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.TestFramework;
using NexusMods.Networking.HttpDownloader.Tests;

namespace NexusMods.Networking.Downloaders.Tests;

public class HttpDownloadTaskTests : AGameTest<SkyrimSpecialEdition>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DownloadService _downloadService;
    private readonly LocalHttpServer _server;

    public HttpDownloadTaskTests(IServiceProvider serviceProvider, DownloadService downloadService, LocalHttpServer server) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _downloadService = downloadService;
        _server = server;
    }

    [Theory]
    [InlineData("Resources/RootedAtGameFolder/-Skyrim 202X 9.0 - Architecture-2347-9-0-1664994366.zip")]
    [InlineData("Resources/RootedAtDataFolder/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    [InlineData("Resources/HasEsp_InSubfolder/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/HasEsp/SkyUI_5_2_SE-12604-5-2SE_partial.zip")]
    [InlineData("Resources/DataFolderWithDifferentName/-Skyrim 202X 9.0 to 9.4 - Update Ravenrock.zip")]
    public async Task DownloadModFromUrl(string url)
    {
        var loadout = await CreateLoadout();
        var task = new HttpDownloadTask(_serviceProvider.GetRequiredService<ILogger<HttpDownloadTask>>(), TemporaryFileManager, _serviceProvider.GetRequiredService<HttpClient>(), HttpDownloader, ArchiveAnalyzer, ArchiveInstaller, _downloadService);
        var origNumMods = loadout.Value.Mods.Count;
        origNumMods.Should().Be(1); // game files

        var makeUrl = $"{_server.Uri}{url}";
        task.Init(makeUrl, loadout.Value);
        await task.StartAsync();
        loadout.Value.Mods.Count.Should().BeGreaterThan(origNumMods);
    }
}
