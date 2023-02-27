using FluentAssertions;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.RedEngine.Tests;

[Trait("RequiresNetworking", "True")]
public class ModInstallerTests
{
    private readonly LoadoutManager _manager;
    private readonly GameInstallation _installation;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly Client _nexusClient;
    private readonly IHttpDownloader _httpDownloader;
    private readonly ArchiveManager _archiveManager;
    private readonly ILogger<ModInstallerTests> _logger;

    public ModInstallerTests(ILogger<ModInstallerTests> logger, LoadoutManager manager, 
        Cyberpunk2077 game, TemporaryFileManager temporaryFileManager,
        Client nexusClient, IHttpDownloader httpDownloader,
        ArchiveManager archiveManager)
    {
        _logger = logger;
        _installation = game.Installations.First();
        _archiveManager = archiveManager;
        _manager = manager;
        _temporaryFileManager = temporaryFileManager;
        _nexusClient = nexusClient;
        _httpDownloader = httpDownloader;
    }
    
    [Theory]
    [MemberData(nameof(TestFiles))]
    public async Task CanCreateLoadout(string name, ModId modId, FileId fileId, Hash hash, int fileCount)
    {
        var loadout = await _manager.ImportFrom(KnownFolders.EntryFolder.CombineUnchecked(@"Resources\cyberpunk2077.1.61.zip"));
        loadout.Value.Mods.Values.Select(m => m.Name).Should().Contain("Game Files");
        var gameFiles = loadout.Value.Mods.Values.First(m => m.Name == "Game Files");
        gameFiles.Files.Count.Should().BeGreaterThan(0);

        var file = await Download(modId, fileId, hash);
        var installedId = await loadout.Install(file, name);
        loadout.Value.Mods[installedId].Files.Count.Should().Be(fileCount);

    }

    private async Task<AbsolutePath> Download(ModId modId, FileId fileId, Hash hash)
    {
        if (_archiveManager.HaveArchive(hash))
            return _archiveManager.PathFor(hash);
            
        _logger.LogInformation("Downloading {ModId} {FileId} {Hash}", modId, fileId, hash);
        var uris = await _nexusClient.DownloadLinks(GameDomain.Cyberpunk2077, modId, fileId);
        
        var file = _temporaryFileManager.CreateFile();
        var downloadHash = await _httpDownloader.Download(uris.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToArray(), file);
        downloadHash.Should().Be(hash);
        return file.Path;
    }

    public static IEnumerable<object[]> TestFiles => new[]
    {
        // new object[] {"Cyber Engine Tweaks", ModId.From(107), FileId.From(33156), Hash.From(0x8BCFFD83A0F5FC71), 16},
        // new object[] {"Redscript", ModId.From(1511), FileId.From(36622), Hash.From(0x8BEF15CA909D8543), 3},
        // new object[] {"cybercmd", ModId.From(5176), FileId.From(34566), Hash.From(0xEE78A096C8565B99), 1},
        // new object[] {"Archive-XL", ModId.From(4198), FileId.From(36969), Hash.From(0x1D30861A46BA7B8E), 2},
        // new object[] {"Tweak-XL", ModId.From(4197), FileId.From(36048), Hash.From(0x22A45B59423201E1), 2},
        // new object[] {"Panam Romanced Enhanced", ModId.From(4626), FileId.From(38117), Hash.From(0xDD0E1C3AEA20E6C1), 3},
        new object[] {"Panam Romanced Enhanced REDmod", ModId.From(4626), FileId.From(38118), Hash.From(0x46F5AD6A75172F76), 5},
        // new object[] {"Tarnished Pack", ModId.From(6072), FileId.From(31953), Hash.From(0x0AAFB35F889500BD), 6},
        // new object[] {"Spicy Valentina", ModId.From(7404), FileId.From(39175), Hash.From(0xB354E2BE032A947F), 2},
        // new object[] {"OVE3RCHROME - Alternative LUT", ModId.From(6579), FileId.From(39289), Hash.From(0xC9CF6070A596BFDA), 2},
        // new object[] {"Hair BUNS PACK", ModId.From(6072), FileId.From(31979), Hash.From(0xAFEABDAF2B38E408), 15},
        // new object[] {"Cyberscript Core", ModId.From(6475), FileId.From(39313), Hash.From(0x0A18B74B0A78188F), 442},
        // new object[] {"Een Glish Radio", ModId.From(6172), FileId.From(39296), Hash.From(0x774BB4AB62B00F50), 29}
    };
}