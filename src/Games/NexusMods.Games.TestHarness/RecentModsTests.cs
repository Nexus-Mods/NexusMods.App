using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Games.TestHarness;

// TODO: Make this code not dead.

public class RecentModsTest
{
    private readonly INexusApiClient _nexusApiClient;
    private readonly ILogger _logger;
    private readonly IDataStore _store;
    private readonly IGame _game;
    private readonly IHttpDownloader _downloader;
    private readonly TimeSpan _updateDelay;
    private AbsolutePath _downloadedFilesLocation;

    public IEnumerable<(NexusModFile FileInfo, AbsolutePath Path)> GameRecords => _gameRecords
        .Select(p => (p, _downloadedFilesLocation.Combine($"{_game.Domain}_{p.ModId}_{p.FileId}")));

    private List<NexusModFile> _gameRecords = new();

    protected RecentModsTest(ILogger logger, INexusApiClient nexusApiClient, IDataStore store, IGame game, IHttpDownloader downloader, IFileSystem fileSystem)
    {
        _store = store;
        _logger = logger;
        _nexusApiClient = nexusApiClient;
        _game = game;
        _downloader = downloader;
        _downloadedFilesLocation = fileSystem.GetKnownPath(KnownPath.EntryDirectory)
            .Combine("DownloadedTestHarnessFiles");
        _downloadedFilesLocation.CreateDirectory();
        _updateDelay = TimeSpan.FromDays(1);
    }

    public async Task Generate()
    {
        var previousRecords = NexusModFile.LoadAll(_store, _game.Domain).ToArray();
        if (previousRecords.Any(p => DateTime.UtcNow - p.LastUpdated < _updateDelay))
        {
            _logger.LogInformation("Skipping update of {Game} popular mods", _game.Name);
            _gameRecords = previousRecords.ToList();
            return;
        }

        var updates = await _nexusApiClient.ModUpdatesAsync(_game.Domain.Value, PastTime.Day);
        _logger.LogInformation("Found {Count} updates", updates.Data.Length);
        var files = new List<(ModId ModId, ModFile File)>();
        foreach (var mod in updates.Data)
        {
            try
            {
                var modFiles = await _nexusApiClient.ModFilesAsync(_game.Domain.Value, mod.ModId);
                files.AddRange(modFiles.Data.Files.Where(f => f.CategoryId == 1).Select(f => (mod.ModId, f)));
            }
            catch (HttpRequestException) { }
        }
        _logger.LogInformation("Found {Count} files totaling {Size}", files.Count, files.Sum(f => f.File.SizeInBytes));

        foreach (var file in files)
        {
            var record = new NexusModFile
            {
                LastUpdated = DateTime.UtcNow,
                FileName = file.File.Name,
                ModId = file.ModId,
                FileId = FileId.From((ulong)file.File.FileId),
                Domain = _game.Domain,
                Hash = Hash.Zero
            };

            _logger.LogInformation("Downloading {FileName}", record.FileName);
            var fileLocation = _downloadedFilesLocation.Combine($"{_game.Domain}_{record.ModId}_{record.FileId}");
            var tempFileLocation = _downloadedFilesLocation.Combine($"{_game.Domain}_{record.ModId}_{record.FileId}.temp");
            var urls = await _nexusApiClient.DownloadLinksAsync(_game.Domain.Value, record.ModId, record.FileId);
            var hash = await _downloader.DownloadAsync(urls.Data.Select(u => new HttpRequestMessage(HttpMethod.Get, u.Uri)).ToList(),
                tempFileLocation);

            await tempFileLocation.MoveToAsync(fileLocation);

            record = record with { Hash = hash };
            record.EnsurePersisted(_store);

        }

        _gameRecords = NexusModFile.LoadAll(_store, _game.Domain).ToList();
    }
}
