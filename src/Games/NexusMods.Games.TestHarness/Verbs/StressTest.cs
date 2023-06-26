using Microsoft.Extensions.Logging;
using NexusMods.CLI;
using NexusMods.CLI.DataOutputs;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.NMA.Extensions;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using ModId = NexusMods.Networking.NexusWebApi.Types.ModId;

namespace NexusMods.Games.TestHarness.Verbs;

public class StressTest : AVerb<IGame, AbsolutePath>
{
    private readonly IRenderer _renderer;
    private readonly Client _client;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly IHttpDownloader _downloader;

    public StressTest(ILogger<StressTest> logger, Configurator configurator,
        LoadoutManager loadoutManager, Client client,
        TemporaryFileManager temporaryFileManager, 
        IHttpDownloader downloader,
        IArchiveAnalyzer archiveAnalyzer,
        IArchiveInstaller archiveInstaller,
        IEnumerable<IGameLocator> gameLocators)
    {
        _archiveAnalyzer = archiveAnalyzer;
        _archiveInstaller = archiveInstaller;
        _downloader = downloader;
        _renderer = configurator.Renderer;
        _loadoutManager = loadoutManager;
        _logger = logger;
        _client = client;
        _temporaryFileManager = temporaryFileManager;
        _manualLocator = gameLocators.OfType<ManuallyAddedLocator>().First();
    }

    public static VerbDefinition Definition =>
        new VerbDefinition("stress-test", "Stress test the application by installing all recent mods for a given game",
            new OptionDefinition[]
            {
                new OptionDefinition<IGame>("g", "game", "The game to install mods for"),
                new OptionDefinition<AbsolutePath>("l", "loadout", "An exported loadout file to use when priming tests"),
                new OptionDefinition<AbsolutePath>("o", "output", "The output file to write the markdown report to")
            });

    private readonly LoadoutManager _loadoutManager;
    private readonly ILogger<StressTest> _logger;
    private readonly IArchiveAnalyzer _archiveAnalyzer;
    private readonly IArchiveInstaller _archiveInstaller;
    private readonly ManuallyAddedLocator _manualLocator;

    public async Task<int> Run(IGame game, AbsolutePath output, CancellationToken token)
    {
        var mods = await _client.ModUpdatesAsync(game.Domain, Client.PastTime.Day, token);
        var results = new List<(string FileName, ModId ModId, FileId FileId, Hash Hash, bool Passed, Exception? exception)>();

        await using var gameFolder = _temporaryFileManager.CreateFolder();
        var gameId = _manualLocator.Add(game, new Version(1, 0), gameFolder);
        game.ResetInstallations();
        var install = game.Installations.First(f => f.Store == GameStore.ManuallyAdded);

        try
        {
            foreach (var mod in mods.Data)
            {
                _logger.LogInformation("Processing {ModId}", mod.ModId);

                Response<ModFiles> files;
                try
                {
                    files = await _client.ModFilesAsync(game.Domain, mod.ModId, token);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogInformation(ex, "Failed to get files for {ModId}", mod.ModId);
                    continue;
                }

                var hash = Hash.Zero;
                foreach (var file in files.Data.Files)
                {
                    try
                    {
                        if (file.CategoryId != 1) continue;
                        _logger.LogInformation("Downloading {ModId} {FileId} {FileName} - {Size}", mod.ModId,
                            file.FileId,
                            file.FileName, file.SizeInBytes);

                        var urls = await _client.DownloadLinksAsync(game.Domain, mod.ModId, file.FileId, token);
                        await using var tmpPath = _temporaryFileManager.CreateFile();

                        var cts = new CancellationTokenSource();
                        cts.CancelAfter(TimeSpan.FromMinutes(20));

                        hash = await _downloader.DownloadAsync(urls.Data.Select(d => d.Uri), tmpPath, token: cts.Token);

                        _logger.LogInformation("Installing {ModId} {FileId} {FileName} - {Size}", mod.ModId,
                            file.FileId,
                            file.FileName, file.SizeInBytes);

                        var list = await _loadoutManager.ManageGameAsync(install,
                            indexGameFiles: false, token: cts.Token);
                        var analysisData = await _archiveAnalyzer.AnalyzeFileAsync(tmpPath, token);
                        await _archiveInstaller.AddMods(list.Value.LoadoutId, analysisData.Hash, token: token);

                        results.Add((file.FileName, mod.ModId, file.FileId, hash, true, null));
                        _logger.LogInformation("Installed {ModId} {FileId} {FileName} - {Size}", mod.ModId, file.FileId,
                            file.FileName, file.SizeInBytes);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to install {ModId} {FileId} {FileName}", mod.ModId, file.FileId,
                            file.FileName);
                        results.Add((file.FileName, mod.ModId, file.FileId, hash, false, ex));
                    }

                }
            }

            var table = new Table(new[] { "Name", "ModId", "FileId", "Hash", "Passed", "Exception" },
                results.Select(r => new object[]
                {
                    r.FileName, r.ModId.ToString(), r.FileId.ToString(), r.Hash, r.Passed.ToString(),
                    r.exception?.Message ?? ""
                }));
            await _renderer.Render(table);

            var lines = new List<string>
            {
                $"# {game.Domain} Test Results - {results.Count} files",
                "",
                "| Status | Name | ModId | FileId | Hash | Exception |",
                "| ---- | ----- | ------ | ---- | ------ | --------- |"
            };
            foreach (var result in results.OrderBy(x => x.Passed)
                         .ThenBy(x => x.ModId)
                         .ThenBy(x => x.FileId))
            {
                var status = result.Passed ? ":white_check_mark:" : ":x:";
                lines.Add(
                    $"| {status} | {result.FileName} | {result.ModId} | {result.FileId} | {result.Hash} | {result.exception?.Message} |");
            }

            if (output != default)
                await output.WriteAllLinesAsync(lines, token);

        }
        finally
        {
            _manualLocator.Remove(gameId);
        }

        return 0;
    }
}
