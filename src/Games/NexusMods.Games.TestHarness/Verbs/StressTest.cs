using Microsoft.Extensions.Logging;
using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.TestHarness.Verbs;

public class StressTest : AVerb<IGame, AbsolutePath>
{
    private readonly IRenderer _renderer;
    private readonly IServiceProvider _provider;
    private readonly Client _client;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IHttpDownloader _downloader;

    public StressTest(ILogger<StressTest> logger, Configurator configurator, IServiceProvider provider, 
        LoadoutManager loadoutManager, FileContentsCache fileContentsCache, Client client, 
        TemporaryFileManager temporaryFileManager, IHttpDownloader downloader)
    {
        _downloader = downloader;
        _fileContentsCache = fileContentsCache;
        _renderer = configurator.Renderer;
        _provider = provider;
        _loadoutManager = loadoutManager;
        _logger = logger;
        _client = client;
        _temporaryFileManager = temporaryFileManager;
    }
    
    public static VerbDefinition Definition => 
        new VerbDefinition("stress-test", "Stress test the application by installing all recent mods for a given game", 
            new OptionDefinition[]
            {
                new OptionDefinition<IGame>("g", "game", "The game to install mods for"),
                new OptionDefinition<AbsolutePath>("l", "loadout", "An exported loadout file to use when priming tests")
            });

    private readonly LoadoutManager _loadoutManager;
    private readonly ILogger<StressTest> _logger;
    private readonly FileContentsCache _fileContentsCache;

    private HashSet<FileType> _skippedTypes = new()
    {
        FileType.PDF
    };

    public async Task<int> Run(IGame game, AbsolutePath loadout, CancellationToken token)
    {
        var mods = await _client.ModUpdates(game.Domain, Client.PastTime.Day, token);
        var results = new List<(string FileName, ModId ModId, FileId FileId, bool Passed, Exception? exception)>();

        foreach (var mod in mods.Data)
        {
            _logger.LogInformation("Processing {ModId}", mod.ModId);

            Response<ModFiles> files;
            try
            {
                files = await _client.ModFiles(game.Domain, mod.ModId, token);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation(ex, "Failed to get files for {ModId}", mod.ModId);
                continue;
            }

            foreach (var file in files.Data.Files)
            {
                try
                {
                    if (file.CategoryId != 1) continue;
                    _logger.LogInformation("Downloading {ModId} {FileId} {FileName} - {Size}", mod.ModId, file.FileId,
                        file.FileName, file.SizeInBytes);

                    var urls = await _client.DownloadLinks(game.Domain, mod.ModId, file.FileId, token);
                    var tmpPath = _temporaryFileManager.CreateFile();

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromMinutes(2));
                    
                    await _downloader.Download(urls.Data.Select(d => d.Uri), tmpPath, token: cts.Token);

                    _logger.LogInformation("Installing {ModId} {FileId} {FileName} - {Size}", mod.ModId, file.FileId,
                        file.FileName, file.SizeInBytes);

                    var list = await _loadoutManager.ImportFrom(loadout, token);
                    await list.Install(tmpPath, "Stress Test Mod", token);
                    
                    results.Add((file.FileName, mod.ModId, file.FileId, true, null));
                    _logger.LogInformation("Installed {ModId} {FileId} {FileName} - {Size}", mod.ModId, file.FileId,
                        file.FileName, file.SizeInBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to install {ModId} {FileId} {FileName}", mod.ModId, file.FileId,
                        file.FileName);
                    results.Add((file.FileName, mod.ModId, file.FileId, false, ex));
                }

            }
        }

        await _renderer.Render(new Table(new[] { "Name", "ModId", "FileId", "Passed", "Exception" },
            results.Select(r => new[]
            {
                r.FileName, r.ModId.ToString(), r.FileId.ToString(), r.Passed.ToString(), r.exception?.Message ?? ""
            })));
        
        return results.Count(f => !f.Passed);
    }
}