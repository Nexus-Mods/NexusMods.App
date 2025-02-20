using System.IO.Compression;
using System.Text.Json;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Extensions.BCL;
using NexusMods.Games.FileHashes.DTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

public class FileHashesService : IFileHashesService, IDisposable
{
    private readonly ScopedAsyncLock _lock = new();
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
            path.DeleteDirectory(true);
        }
        
        // Open the latest database
        var latest = ExistingDBs().FirstOrDefault();
        if (latest.Path != default(AbsolutePath))
        {
            _currentDb = OpenDb(latest.PublishTime, latest.Path);
        }
        
        // Trigger an update
        Task.Run(() => Task.FromResult(CheckForUpdate()));
    }

    private ConnectedDb OpenDb(DateTimeOffset timestamp, AbsolutePath path)
    {
        try
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
            var connection = new Connection(_provider.GetRequiredService<ILogger<Connection>>(), store, _provider,
                []
            , readOnlyMode: true);


            var connectedDb = new ConnectedDb(connection.Db, connection, store,
                backend, timestamp, path
            );
            _databases[path] = connectedDb;
            return connectedDb;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening database at {Path}", path);
            throw;
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
                    // Format is "{guid}_{timestamp}"
                    var parts = d.FileName.Split('_');
                    if (parts.Length != 2 || !ulong.TryParse(parts[1], out var timestamp))
                        return default((DateTimeOffset, AbsolutePath));
                    return (DateTimeOffset.FromUnixTimeSeconds((long)timestamp), d);
                })
            .Where(v => v != default((DateTimeOffset, AbsolutePath)))
            .OrderByDescending(v => v.Item1);
    }
    
    private async Task<Manifest> GetRelease(AbsolutePath storagePath)
    {
        await using var stream = await _httpClient.GetStreamAsync(_settings.GithubManifestUrl);
        await using var diskPath = storagePath.Create();
        await stream.CopyToAsync(diskPath);
        diskPath.Position = 0;
        return (await JsonSerializer.DeserializeAsync<Manifest>(diskPath, _jsonSerializerOptions))!;
    }

    /// <inheritdoc />
    public async Task CheckForUpdate(bool forceUpdate = false)
    {
        await CheckForUpdateCore(forceUpdate);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetGameVersions(GameInstallation installation)
    {
        return VersionDefinition.All(Current)
            .Where(v => v.GameId == installation.Game.GameId)
            .Select(v => v.Name)
            .ToList();
    }
    
    public IEnumerable<VersionDefinition.ReadOnly> GetVersionDefinitions(GameInstallation installation)
    {
        return VersionDefinition.All(Current)
            .Where(v => v.GameId == installation.Game.GameId)
            .ToList();
    }

    private async Task CheckForUpdateCore(bool forceUpdate)
    {
        using var _ = await _lock.LockAsync();
        var gameHashesReleaseFileName = GameHashesReleaseFileName;
        if (!forceUpdate)
        {
            if (gameHashesReleaseFileName.FileExists && gameHashesReleaseFileName.FileInfo.LastWriteTimeUtc + _settings.HashDatabaseUpdateInterval > DateTime.UtcNow)
            {
                _logger.LogTrace("Skipping update check due a check limit of {CheckIterval}", _settings.HashDatabaseUpdateInterval);
                var latest = ExistingDBs().FirstOrDefault();
                if (latest.Path != default(AbsolutePath))
                {
                    _currentDb = OpenDb(latest.PublishTime, latest.Path);
                    return;
                }
            }
        }

        var release = await GetRelease(gameHashesReleaseFileName);
        
        _logger.LogTrace("Checking for new hash database release");

        var existingReleases = ExistingDBs().ToList();

        if (existingReleases.Any(r => r.PublishTime == release.CreatedAt))
            return;

        var guid = Guid.NewGuid().ToString();
        var tempZipPath = _settings.HashDatabaseLocation.ToPath(_fileSystem) / $"{guid}.{release.CreatedAt.ToUnixTimeSeconds()}.zip";
        
        {
            // download the database
            await using var stream = await _httpClient.GetStreamAsync(_settings.GameHashesDbUrl);
            await using var fileStream = tempZipPath.Create();
            await stream.CopyToAsync(fileStream);
        }


        var tempDir = _settings.HashDatabaseLocation.ToPath(_fileSystem) / $"{guid}_{release.CreatedAt.ToUnixTimeSeconds()}_tmp";
        {
            // extract it 
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
        var finalDir = _settings.HashDatabaseLocation.ToPath(_fileSystem) / (guid + "_" + release.CreatedAt.ToUnixTimeSeconds());
        Directory.Move(tempDir.ToString(), finalDir.ToString());

        // delete the temp files
        tempZipPath.Delete();
        
        // open the new database
        _currentDb = OpenDb(release.CreatedAt, finalDir);
    }

    private AbsolutePath GameHashesReleaseFileName => _settings.HashDatabaseLocation.ToPath(_fileSystem) / _settings.ReleaseFilePath;

    /// <inheritdoc />
    public async ValueTask<IDb> GetFileHashesDb()
    {
        if (_currentDb is not null)
            return _currentDb.Db;

        // Call core since we're already inside a lock
        await CheckForUpdateCore(false);

        return Current;
    }
    public IEnumerable<GameFileRecord> GetGameFiles(GameInstallation installation, IEnumerable<string> locatorIds)
    {
        if (installation.Store == GameStore.GOG)
        {
            foreach (var id in locatorIds)
            {
                if (!ulong.TryParse(id, out var parsedId))
                    continue;
                
                var gogId = BuildId.From(parsedId);

                if (!GogBuild.FindByBuildId(Current, gogId).TryGetFirst(out var firstBuild))
                    continue;

                foreach (var file in firstBuild.Files)
                {
                    yield return new GameFileRecord
                    {
                        Path = (LocationId.Game, file.Path),
                        Size = file.Hash.Size,
                        MinimalHash = file.Hash.MinimalHash,
                        Hash = file.Hash.XxHash3,
                    };
                }
            }
        }
        else if (installation.Store == GameStore.Steam)
        {
            foreach (var id in locatorIds)
            {
                if (!ulong.TryParse(id, out var parsedId))
                    continue;
                
                var manifestId = ManifestId.From(parsedId);

                if (!SteamManifest.FindByManifestId(Current, manifestId).TryGetFirst(out var firstManifest))
                    continue;

                foreach (var file in firstManifest.Files)
                {
                    yield return new GameFileRecord
                    {
                        Path = (LocationId.Game, file.Path),
                        Size = file.Hash.Size,
                        MinimalHash = file.Hash.MinimalHash,
                        Hash = file.Hash.XxHash3,
                    };
                }
            }
        }
        else
        {
            throw new NotImplementedException("No way to get game files for: " + installation.Store);
        }
    }

    /// <inheritdoc />
    public IDb Current => _currentDb?.Db ?? throw new InvalidOperationException("No database connected");

    /// <inheritdoc />
    public bool TryGetGameVersion(GameInstallation installation, IEnumerable<string> locatorMetadata, out string version)
    {
        if (TryGetGameVersionDefinition(installation, locatorMetadata, out var versionDefinition))
        {
            version = versionDefinition.Name;
            return true;
        }

        version = string.Empty;
        return false;
    }
    private bool TryGetGameVersionDefinition(GameInstallation installation, IEnumerable<string> locatorMetadata, out VersionDefinition.ReadOnly versionDefinition)
    {
        versionDefinition = default(VersionDefinition.ReadOnly);
        if (installation.Store == GameStore.GOG)
        {
            List<GogBuild.ReadOnly> gogBuilds = [];

            foreach (var gogId in locatorMetadata)
            {
                if (!ulong.TryParse(gogId, out var parsedId))
                {
                    _logger.LogWarning("Unable to parse `{Raw}` as ulong", gogId);
                    return false;
                }

                var gogBuild = GogBuild.FindByBuildId(Current, BuildId.From(parsedId))
                    .FirstOrDefault();
                gogBuilds.Add(gogBuild);
            }

            if (gogBuilds.Count == 0)
            {
                _logger.LogDebug("No GOG builds found for locator metadata");
                return false;
            }

            var wasFound = VersionDefinition.All(_currentDb!.Db)
                .Select(version =>
                    {
                        var matchingIdCount = gogBuilds.Count(g => version.GogBuildsIds.Contains(g));
                        return (matchingIdCount, version);
                    })
                .Where(row => row.matchingIdCount > 0)
                .OrderByDescending(row => row.matchingIdCount)
                .Select(t => t.version)
                .TryGetFirst(out versionDefinition);

            if (!wasFound)
            {
                _logger.LogDebug("No version found for locator metadata");
                return false;
            }
        }
        else if (installation.Store == GameStore.Steam)
        {
            List<SteamManifest.ReadOnly> steamManifests = [];
            
            foreach (var steamId in locatorMetadata)
            {
                if (!ulong.TryParse(steamId, out var parsedId))
                {
                    _logger.LogDebug("Steam locator {0} metadata is not a valid ulong", steamId);
                    return false;
                }

                var steamManifest = SteamManifest.FindByManifestId(Current, ManifestId.From(parsedId))
                    .FirstOrDefault();
                steamManifests.Add(steamManifest);
            }

            if (steamManifests.Count == 0)
            {
                _logger.LogDebug("No Steam manifests found for locator metadata");
                return false;
            }
            
            var wasFound = VersionDefinition.All(_currentDb!.Db)
                .Select(version =>
                    {
                        var matchingIdCount = steamManifests.Count(g => version.SteamManifestsIds.Contains(g));
                        return (matchingIdCount, version);
                    })
                .Where(row => row.matchingIdCount > 0)
                .OrderByDescending(row => row.matchingIdCount)
                .Select(t => t.version)
                .TryGetFirst(out versionDefinition);

            if (!wasFound)
            {
                _logger.LogDebug("No version found for locator metadata");
                return false;
            }
        }
        else
        {
            _logger.LogDebug("No way to get game version for: {Store}", installation.Store);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool TryGetLocatorIdsForVersion(GameInstallation gameInstallation, string version, out string[] commonIds)
    {
        if (gameInstallation.Store == GameStore.GOG)
        {
            if (!VersionDefinition.FindByName(Current, version).TryGetFirst(out var versionDef))
            {
                commonIds = [];
                return false;
            }

            commonIds = versionDef.GogBuilds.Select(build => build.BuildId.ToString()).ToArray();
            return true;
        }
        else if (gameInstallation.Store == GameStore.Steam)
        {
            if (!VersionDefinition.FindByName(Current, version).TryGetFirst(out var versionDef))
            {
                commonIds = [];
                return false;
            }

            commonIds = versionDef.SteamManifests.Select(manifest => manifest.ManifestId.ToString()).ToArray();
            return true;
        }
        else
        {
            throw new NotImplementedException("No way to get common IDs for: " + gameInstallation.Store);
        }
    }

    public string[] GetLocatorIdsForVersionDefinition(GameInstallation gameInstallation, VersionDefinition.ReadOnly versionDefinition)
    {
        if (gameInstallation.Store == GameStore.GOG)
        {
            return versionDefinition.GogBuilds.Select(build => build.BuildId.ToString()).ToArray();
        }
        else if (gameInstallation.Store == GameStore.Steam)
        {
            return versionDefinition.SteamManifests.Select(manifest => manifest.ManifestId.ToString()).ToArray();
        }
        else
        {
            throw new NotImplementedException("No way to get common IDs for: " + gameInstallation.Store);
        }
    }


    /// <inheritdoc />
    public string SuggestGameVersion(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files)
    {
        var filesSet = files.ToHashSet();

        List<(string Version, int Matches)> versionMatches = [];
        foreach (var version in GetGameVersions(gameInstallation))
        {
            if (!TryGetLocatorIdsForVersion(gameInstallation, version, out var commonIds))
                continue;

            var matchingCount = GetGameFiles(gameInstallation, commonIds)
                .Count(file => filesSet.Contains((file.Path, file.Hash)));
            
            versionMatches.Add((version, matchingCount));
        }
        
        return versionMatches
            .OrderByDescending(t => t.Matches)
            .Select(t => t.Version)
            .FirstOrDefault() ?? string.Empty;
    }

    /// <inheritdoc />
    public Optional<VersionData> SuggestVersionDefinitions(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files)
    {
        var filesSet = files.ToHashSet();
        
        List<(VersionData VersionData, int Matches)> versionMatches = [];
        foreach (var versionDefinition in GetVersionDefinitions(gameInstallation))
        {
            var commonIds = GetLocatorIdsForVersionDefinition(gameInstallation, versionDefinition);

            var matchingCount = GetGameFiles(gameInstallation, commonIds)
                .Count(file => filesSet.Contains((file.Path, file.Hash)));
            
            versionMatches.Add((new VersionData(commonIds, versionDefinition.Name), matchingCount));
        }
        
        return versionMatches
            .OrderByDescending(t => t.Matches)
            .Select(t => t.VersionData)
            .FirstOrOptional(item => true);
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
