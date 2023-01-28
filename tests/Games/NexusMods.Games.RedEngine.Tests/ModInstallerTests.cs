using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using Noggog;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.RedEngine.Tests;

[Trait("RequiresGameInstalls", "True")]
public class ModInstallerTests
{
    private readonly LoadoutManager _manager;
    private readonly GameInstallation _installation;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly Client _nexusClient;
    private readonly IHttpDownloader _httpDownloader;

    public ModInstallerTests(ILogger<ModInstallerTests> logger, LoadoutManager manager, 
        Cyberpunk2077 game, TemporaryFileManager temporaryFileManager,
        Client nexusClient, IHttpDownloader httpDownloader)
    {
        _installation = game.Installations.First();
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
        _nexusClient = nexusClient;
        _httpDownloader = httpDownloader;
    }
    
    [Theory]
    [MemberData(nameof(TestFiles))]
    public async Task CanCreateLoadout(string name, ModId modId, FileId fileId, Hash hash, int fileCount)
    {
        var loadout = await _manager.ManageGame(_installation, Guid.NewGuid().ToString());
        loadout.Value.Mods.Values.Select(m => m.Name).Should().Contain("Game Files");
        var gameFiles = loadout.Value.Mods.Values.First(m => m.Name == "Game Files");
        gameFiles.Files.Count.Should().BeGreaterThan(0);

        var file = await Download(modId, fileId, hash);
        var installedId = await loadout.Install(file, "Cyber Engine Tweaks");
        loadout.Value.Mods[installedId].Files.Count.Should().Be(fileCount);

    }

    private async Task<AbsolutePath> Download(ModId modId, FileId fileId, Hash hash)
    {
        var uris = await _nexusClient.DownloadLinks(GameDomain.Cyberpunk2077, modId, fileId);
        
        var file = _temporaryFileManager.CreateFile();
        var downloadHash = await _httpDownloader.Download(uris.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(), file);
        downloadHash.Should().Be(hash);
        return file.Path;
    }

    public static IEnumerable<object[]> TestFiles => new[]
    {
        new object[] {"Cyber Engine Tweaks", ModId.From(107), FileId.From(33156), Hash.From(0x8BCFFD83A0F5FC71), 17},
        new object[] {"Redscript", ModId.From(1511), FileId.From(36622), Hash.From(0x8BEF15CA909D8543), 3},
        new object[] {"cybercmd", ModId.From(5176), FileId.From(34566), Hash.From(0xEE78A096C8565B99), 1},
        new object[] {"Archive-XL", ModId.From(4198), FileId.From(36969), Hash.From(0x1D30861A46BA7B8E), 2},
        new object[] {"Tweak-XL", ModId.From(4197), FileId.From(36048), Hash.From(0x22A45B59423201E1), 2},
    };
}