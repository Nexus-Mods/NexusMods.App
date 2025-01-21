using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Settings;
using NexusMods.Games.FileHashes.GithubDTOs;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

public class FileHashesService : IFileHashesService, IDisposable
{
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private readonly FileHashesServiceSettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Dictionary<AbsolutePath, ConnectedDb> _databases;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// The currently connected database (if any)
    /// </summary>
    private ConnectedDb? _currentDb;

    private readonly ILogger<FileHashesService> _logger;

    private record ConnectedDb(IDb Db, Connection Connection, DatomStore Store, Backend Backend, DateTimeOffset Timestamp, AbsolutePath Path);

    public FileHashesService(ILogger<FileHashesService> logger, ISettingsManager settingsManager, IFileSystem fileSystem, HttpClient httpClient, JsonSerializerOptions jsonSerializerOptions, IServiceProvider provider)
    {
        _logger = logger;
        _httpClient = httpClient;
        _jsonSerializerOptions = jsonSerializerOptions;
        _fileSystem = fileSystem;
        _settings = settingsManager.Get<FileHashesServiceSettings>();
        _databases = new Dictionary<AbsolutePath, ConnectedDb>();
        _provider = provider;
        
        _settings.HashDatabaseLocation.ToPath(_fileSystem).CreateDirectory();
 
        // Delete any old databases that are not the latest
        // we only delete at startup to avoid race conditions, but 
        // the db doesn't update often so this is fine
        foreach (var (_, path) in ExistingDBs().Skip(1))
        {
            path.DeleteDirectory();
        }


        
        // Open the latest database
        var latest = ExistingDBs().FirstOrDefault();
        if (latest.Path != default(AbsolutePath))
        {
            _currentDb = OpenDb(latest.PublishTime, latest.Path);
        }
        
        // Trigger an update
        Task.Run(() => Task.FromResult(ForceUpdate()));
    }

    private ConnectedDb OpenDb(DateTimeOffset timestamp, AbsolutePath path)
    {
        lock (_databases)
        {
            if (_databases.TryGetValue(path, out var existing))
                return existing;
            
            _logger.LogInformation("Opening hash database at {Path} for {Timestamp}", path, timestamp);
            var backend = new Backend(readOnly: true);
            var settings = new DatomStoreSettings
            {
                Path = path,
            };
            var store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
            var connection = new Connection(_provider.GetRequiredService<ILogger<Connection>>(), store, _provider, []);


            var connectedDb = new ConnectedDb(connection.Db, connection, store, backend, timestamp, path);
            _databases[path] = connectedDb;
            return connectedDb;
        }
    }
    
    private IEnumerable<(DateTimeOffset PublishTime, AbsolutePath Path)> ExistingDBs()
    {
        return _settings.HashDatabaseLocation
            .ToPath(_fileSystem)
            .EnumerateDirectories(recursive: false)
            .Where(d => !d.FileName.EndsWith("_tmp"))
            .Select(d =>
                {
                    if (!ulong.TryParse(d.FileName, out var timestamp))
                        return default((DateTimeOffset, AbsolutePath));
                    return (DateTimeOffset.FromUnixTimeSeconds((long)timestamp), d);
                })
            .Where(v => v != default((DateTimeOffset, AbsolutePath)))
            .OrderByDescending(v => v.Item1);
    }
    
    private async Task<Release> GetRelease()
    {
        await using var stream = await _httpClient.GetStreamAsync(_settings.GithubReleaseUrl);
        return (await JsonSerializer.DeserializeAsync<Release>(stream, _jsonSerializerOptions))!;
    }
    
    public async Task ForceUpdate()
    {
        try
        {
            await _updateLock.WaitAsync();
            
            var release = await GetRelease();
            
            _logger.LogTrace("Checking for new hash database release");

            var existingReleases = ExistingDBs().ToList();

            if (existingReleases.Any(r => r.PublishTime == release.PublishedAt))
                return;

            var tempZipPath = _settings.HashDatabaseLocation.ToPath(_fileSystem) / $"{release.PublishedAt.ToUnixTimeSeconds()}.tmp.zip";

            var asset = release.Assets.First(a => a.Name == "game_hashes_db.zip");

            {
                // download the database
                await using var stream = await _httpClient.GetStreamAsync(asset.BrowserDownloadUrl);
                await using var fileStream = tempZipPath.Create();
                await stream.CopyToAsync(fileStream);
            }


            var tempDir = _settings.HashDatabaseLocation.ToPath(_fileSystem) / $"{release.PublishedAt.ToUnixTimeSeconds()}_tmp";
            {
                // extact it 
                tempDir.CreateDirectory();
                using var archive = new ZipArchive(tempZipPath.Read(), ZipArchiveMode.Read, leaveOpen: false);
                foreach (var file in archive.Entries)
                {
                    var filePath = tempDir.Combine(file.FullName);
                    filePath.Parent.CreateDirectory();
                    await using var entryStream = file.Open();
                    await using var fileStream = filePath.Create();
                    await entryStream.CopyToAsync(fileStream);
                }
            }

            // rename the temp folder
            var finalDir = _settings.HashDatabaseLocation.ToPath(_fileSystem) / release.PublishedAt.ToUnixTimeSeconds().ToString();
            Directory.Move(tempDir.ToString(), finalDir.ToString());

            // delete the temp files
            tempZipPath.Delete();
            
            // open the new database
            _currentDb = OpenDb(release.PublishedAt, finalDir);
        }
        finally
        {
            _updateLock.Release();
        }
    }

    public async ValueTask<IDb> GetFileHashesDb()
    {
        if (_currentDb is not null)
            return _currentDb.Db;

        await ForceUpdate();

        return _currentDb!.Db;
    }

    public void Dispose()
    {
        foreach (var connection in _databases.Values)
        {
            connection.Backend.Dispose();
            connection.Store.Dispose();
        }
    }
}
