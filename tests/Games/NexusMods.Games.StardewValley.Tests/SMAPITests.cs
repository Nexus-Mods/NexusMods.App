using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.StardewValley.Tests;
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

[Trait("RequiresNetworking", "True")]
public class SMAPITests
{
    private ILogger<SMAPITests> _logger;
    private GameInstallation _installation;
    private ArchiveManager _archiveManager;
    private LoadoutManager _manager;
    private TemporaryFileManager _temporaryFileManager;
    private Client _nexusClient;
    private IHttpDownloader _httpDownloader;
    private IDataStore _dataStore;

    public SMAPITests(ILogger<SMAPITests> logger, LoadoutManager manager,
        StardewValley game, TemporaryFileManager temporaryFileManager,
        Client nexusClient, IHttpDownloader httpDownloader,
        ArchiveManager archiveManager, IDataStore dataStore)
    {
        _logger = logger;
        _installation = game.Installations.First();
        _archiveManager = archiveManager;
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
        _nexusClient = nexusClient;
        _httpDownloader = httpDownloader;
        _dataStore = dataStore;
    }

    [Fact]
    public async Task Test_SMAPI()
    {
        var loadout = await _manager.ManageGameAsync(_installation, Guid.NewGuid().ToString("N"));

        // SMAPI 3.18.2
        var links = await _nexusClient.DownloadLinks(StardewValley.GameDomain, ModId.From(2400), FileId.From(64874));
        await using var file = _temporaryFileManager.CreateFile();

        var downloadHash = await _httpDownloader.DownloadAsync(links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(), file);
        downloadHash.Should().Be(Hash.From(0x8F3F6450139866F3));

        var modId = await loadout.InstallModAsync(file.Path, "SMAPI");
        var files = loadout.Value.Mods[modId].Files;
        files.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Test_SMAPIMod()
    {
        var loadout = await _manager.ManageGameAsync(_installation, Guid.NewGuid().ToString("N"));

        // NPC Map Locations 2.11.3
        var links = await _nexusClient.DownloadLinks(StardewValley.GameDomain, ModId.From(239), FileId.From(68865));
        await using var file = _temporaryFileManager.CreateFile();

        var downloadHash = await _httpDownloader.DownloadAsync(links.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(), file);
        downloadHash.Should().Be(Hash.From(0x59112FD2E58BD042));

        var modId = await loadout.InstallModAsync(file.Path, "NPC Map Locations");
        var files = loadout.Value.Mods[modId].Files;
        files.Should().NotBeEmpty();
    }
}
