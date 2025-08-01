using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.EpicGameStore.Models;
using NexusMods.Abstractions.EpicGameStore.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Games.FileHashes.DTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.IO;
using BuildId = NexusMods.Abstractions.GOG.Values.BuildId;

namespace NexusMods.Games.FileHashes;

internal sealed class FileHashesService : IFileHashesService, IDisposable
{
    private readonly ScopedAsyncLock _lock = new();
    private readonly FileHashesServiceSettings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly Dictionary<AbsolutePath, ConnectedDb> _databases;
    private readonly IServiceProvider _provider;
    private readonly AbsolutePath _hashDatabaseLocation;

    /// <summary>
    /// The currently connected database (if any)
    /// </summary>
    private ConnectedDb? _currentDb;

    private readonly ILogger<FileHashesService> _logger;

    private record ConnectedDb(IDb Db, DatomStore Store, Backend Backend, DatabaseInfo DatabaseInfo);

    public FileHashesService(ILogger<FileHashesService> logger, ISettingsManager settingsManager, IFileSystem fileSystem, HttpClient httpClient, JsonSerializerOptions jsonSerializerOptions, IServiceProvider provider)
    {
        _logger = logger;
        _httpClient = httpClient;
        _jsonSerializerOptions = jsonSerializerOptions;
        _fileSystem = fileSystem;
        _settings = settingsManager.Get<FileHashesServiceSettings>();
        _databases = new Dictionary<AbsolutePath, ConnectedDb>();
        _provider = provider;

        _hashDatabaseLocation = _settings.HashDatabaseLocation.ToPath(_fileSystem);
        _hashDatabaseLocation.CreateDirectory();

        Startup();
    }

    private ConnectedDb OpenDb(DatabaseInfo databaseInfo)
    {
        try
        {
            if (_databases.TryGetValue(databaseInfo.Path, out var existing))
                return existing;

            _logger.LogInformation("Opening hash database at {Path} for {Timestamp}", databaseInfo.Path, databaseInfo.CreationTime);
            var backend = new Backend(readOnly: true);
            var settings = new DatomStoreSettings
            {
                Path = databaseInfo.Path,
            };

            var store = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
            var connection = new Connection(_provider.GetRequiredService<ILogger<Connection>>(), store, _provider, [], readOnlyMode: true);
            var connectedDb = new ConnectedDb(connection.Db, store, backend, databaseInfo);

            _databases[databaseInfo.Path] = connectedDb;
            return connectedDb;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening database at {Path}", databaseInfo.Path);
            throw;
        }
    }

    private record struct DatabaseInfo(AbsolutePath Path, DateTimeOffset CreationTime);

    private IEnumerable<DatabaseInfo> ExistingDBs()
    {
        return _hashDatabaseLocation
            .EnumerateDirectories(recursive: false)
            .Where(d => !d.FileName.EndsWith("_tmp"))
            .Select(path =>
            {
                // Format is "{guid}_{timestamp}"
                var parts = path.FileName.Split('_');
                if (parts.Length != 2 || !ulong.TryParse(parts[1], out var timestamp)) return Optional<DatabaseInfo>.None;
                return new DatabaseInfo(Path: path, CreationTime: DateTimeOffset.FromUnixTimeSeconds((long)timestamp));
            })
            .Where(static optional => optional.HasValue)
            .Select(static optional => optional.Value)
            .OrderByDescending(static databaseInfo => databaseInfo.CreationTime);
    }

    /// <inheritdoc />
    public async Task CheckForUpdate(bool forceUpdate = false)
    {
        await CheckForUpdateCore(forceUpdate);
    }

    /// <inheritdoc />
    public IEnumerable<VanityVersion> GetKnownVanityVersions(GameId gameId)
    {
        return GetVersionDefinitions(gameId)
            .Select(v => VanityVersion.From(v.Name))
            .ToList();
    }

    private List<VersionDefinition.ReadOnly> GetVersionDefinitions(GameId gameId)
    {
        return VersionDefinition.All(Current)
            .Where(v => v.GameId == gameId)
            .ToList();
    }

    private bool ShouldCheckForUpdate()
    {
        if (!GameHashesReleaseFileName.FileExists) return true;
        var lastUpdated = GameHashesReleaseFileName.FileInfo.LastWriteTimeUtc;
        var diff = DateTime.UtcNow - lastUpdated;
        return diff >= _settings.HashDatabaseUpdateInterval;
    }

    private void Startup()
    {
        var existingDatabases = ExistingDBs().ToArray();

        // Cleanup old databases
        foreach (var databaseInfo in existingDatabases.Skip(1))
        {
            databaseInfo.Path.DeleteDirectory(true);
        }

        if (existingDatabases.TryGetFirst(out var latestDatabase))
        {
            _currentDb = OpenDb(latestDatabase);
        }

        Task.Run(async () => await CheckForUpdateCore(forceUpdate: false, cancellationToken: CancellationToken.None));
    }

    private async Task CheckForUpdateCore(bool forceUpdate, CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.LockAsync();

        var existingDatabases = ExistingDBs().ToArray();
        var shouldCheckForUpdate = forceUpdate || existingDatabases.Length == 0 || ShouldCheckForUpdate();

        if (!shouldCheckForUpdate && existingDatabases.TryGetFirst(out var latestDatabase))
        {
            _currentDb = OpenDb(latestDatabase);
            return;
        }

        Manifest? latestReleaseManifest = null;
        if (shouldCheckForUpdate)
        {
            latestReleaseManifest = await FetchLatestReleaseManifest(GameHashesReleaseFileName, cancellationToken: cancellationToken);
        }

        if (existingDatabases.Length == 0)
        {
            var embeddedDatabaseInfo = await AddEmbeddedDatabase(cancellationToken: cancellationToken);
            if (latestReleaseManifest is null)
            {
                if (!embeddedDatabaseInfo.HasValue)
                {
                    _logger.LogError("Failed to add embedded game hashes database and failed to fetch latest release manifest, game hashes functionality will be unavailable");
                    return;
                }

                _logger.LogWarning("Failed to fetch latest release manifest, defaulting to embedded game hashes database which may be out-of-date");
                _currentDb = OpenDb(embeddedDatabaseInfo.Value);
                return;
            }

            Debug.Assert(latestReleaseManifest is not null, "should've returned if we didn't have a manifest");
            if (embeddedDatabaseInfo.HasValue)
            {
                existingDatabases = ExistingDBs().ToArray();
                Debug.Assert(existingDatabases.Length == 1);
            }
        }

        if (latestReleaseManifest is null && existingDatabases.Length == 0)
        {
            _logger.LogError("Failed to fetch the latest release manifest and failed to use the embedded database, game hashes functionality will be unavailable");
            return;
        }

        if (latestReleaseManifest is null || existingDatabases[0].CreationTime >= latestReleaseManifest.CreatedAt)
        {
            _currentDb = OpenDb(existingDatabases[0]);
            return;
        }

        _logger.LogInformation("Fetching latest games hashes database");
        var releaseDatabaseInfo = await AddReleaseDatabase(latestReleaseManifest, cancellationToken);
        if (!releaseDatabaseInfo.HasValue) return;

        _currentDb = OpenDb(releaseDatabaseInfo.Value);
    }

    private async ValueTask<Manifest?> FetchLatestReleaseManifest(AbsolutePath storagePath, CancellationToken cancellationToken)
    {
        const int defaultTimeout = 15;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(delay: TimeSpan.FromSeconds(defaultTimeout));

        try
        {
            await using var fileStream = storagePath.Create();
            await using (var httpStream = await _httpClient.GetStreamAsync(_settings.GithubManifestUrl, cancellationToken: cts.Token))
            {
                await httpStream.CopyToAsync(fileStream, cancellationToken: cts.Token);
            }

            fileStream.Position = 0;
            return await JsonSerializer.DeserializeAsync<Manifest>(fileStream, _jsonSerializerOptions, cancellationToken: cts.Token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch latest release manifest from `{Url}`", _settings.GithubManifestUrl);
            return null;
        }
    }

    private async ValueTask<Optional<DatabaseInfo>> AddEmbeddedDatabase(CancellationToken cancellationToken)
    {
        try
        {
            var streamFactory = new EmbeddedResourceStreamFactory<FileHashesService>(resourceName: "games_hashes_db.zip");
            await using var archiveStream = await streamFactory.GetStreamAsync();
            var creationTime = ApplicationConstants.BuildDate;

            var path = await AddDatabase(archiveStream, creationTime, cancellationToken);
            return new DatabaseInfo(path, creationTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add embedded database");
            return Optional<DatabaseInfo>.None;
        }
    }

    private async ValueTask<Optional<DatabaseInfo>> AddReleaseDatabase(Manifest releaseManifest, CancellationToken cancellationToken)
    {
        try
        {
            await using var httpStream = await _httpClient.GetStreamAsync(_settings.GameHashesDbUrl, cancellationToken: cancellationToken);
            var path = await AddDatabase(httpStream, releaseManifest.CreatedAt, cancellationToken: cancellationToken);
            return new DatabaseInfo(path, releaseManifest.CreatedAt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to add release database from {Url}", _settings.GameHashesDbUrl);
            return Optional<DatabaseInfo>.None;
        }
    }

    private async ValueTask<AbsolutePath> AddDatabase(
        Stream archiveStream,
        DateTimeOffset databaseCreationTime,
        CancellationToken cancellationToken)
    {
        var name = $"{Guid.NewGuid()}_{databaseCreationTime.ToUnixTimeSeconds()}";

        await using var archivePath = new TemporaryPath(_fileSystem, _hashDatabaseLocation / $"{name}.zip");
        await using (var fileStream = archivePath.Path.Create())
        {
            await archiveStream.CopyToAsync(fileStream, cancellationToken: cancellationToken);
        }

        await using var extractionDirectory = new TemporaryPath(_fileSystem, _hashDatabaseLocation / $"{name}_tmp");
        await using (var fileStream = archivePath.Path.Read())
        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
        {
            foreach (var fileEntry in zipArchive.Entries)
            {
                var destinationPath = extractionDirectory.Path.Combine(fileEntry.FullName);
                destinationPath.Parent.CreateDirectory();

                await using var entryStream = fileEntry.Open();
                await using var outputStream = destinationPath.Create();
                await entryStream.CopyToAsync(outputStream, cancellationToken: cancellationToken);
            }
        }

        var finalDirectory = _hashDatabaseLocation / name;
        Directory.Move(
            sourceDirName: extractionDirectory.Path.ToNativeSeparators(OSInformation.Shared),
            destDirName: finalDirectory.ToNativeSeparators(OSInformation.Shared)
        );

        return finalDirectory;
    }

    private AbsolutePath GameHashesReleaseFileName => _hashDatabaseLocation / _settings.ReleaseFilePath;

    /// <inheritdoc />
    public async ValueTask<IDb> GetFileHashesDb()
    {
        if (_currentDb is not null)
            return _currentDb.Db;

        // Call core since we're already inside a lock
        await CheckForUpdateCore(false);

        return Current;
    }

    /// <inheritdoc/>
    public IEnumerable<GameFileRecord> GetGameFiles(LocatorIdsWithGameStore locatorIdsWithGameStore)
    {
        var (gameStore, locatorIds) = locatorIdsWithGameStore;

        if (gameStore == GameStore.GOG)
        {
            foreach (var id in locatorIds)
            {
                if (!ulong.TryParse(id.Value, out var parsedId))
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
        else if (gameStore == GameStore.Steam)
        {
            foreach (var id in locatorIds)
            {
                if (!ulong.TryParse(id.Value, out var parsedId))
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
        else if (gameStore == GameStore.EGS)
        {
            foreach (var manifestId in locatorIds)
            {
                var egManifestId = ManifestHash.FromUnsanitized(manifestId.Value);
                
                if (!EpicGameStoreBuild.FindByManifestHash(Current, egManifestId).TryGetFirst(out var firstManifest))
                {
                    _logger.LogWarning("No EGS manifest found for {ManifestId}", egManifestId.Value);
                    continue;
                }
                
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
            throw new NotSupportedException("No way to get game files for: " + gameStore);
        }
    }

    /// <inheritdoc />
    public IDb Current => _currentDb?.Db ?? throw new InvalidOperationException("No database connected");

    /// <inheritdoc />
    public bool TryGetVanityVersion(LocatorIdsWithGameStore locatorIdsWithGameStore, out VanityVersion version)
    {
        if (TryGetGameVersionDefinition(locatorIdsWithGameStore, out var versionDefinition))
        {
            version = VanityVersion.From(versionDefinition.Name);
            return true;
        }

        version = VanityVersion.DefaultValue;
        return false;
    }

    private bool TryGetGameVersionDefinition(
        LocatorIdsWithGameStore locatorIdsWithGameStore,
        out VersionDefinition.ReadOnly versionDefinition)
    {
        var (gameStore, locatorIds) = locatorIdsWithGameStore;

        versionDefinition = default(VersionDefinition.ReadOnly);
        if (gameStore == GameStore.GOG)
        {
            List<GogBuild.ReadOnly> gogBuilds = [];

            foreach (var gogId in locatorIds)
            {
                if (!ulong.TryParse(gogId.Value, out var parsedId))
                {
                    _logger.LogWarning("Unable to parse `{Raw}` as ulong", gogId);
                    return false;
                }

                var hasBuild = GogBuild.FindByBuildId(Current, BuildId.From(parsedId)).TryGetFirst(out var gogBuild);
                if (hasBuild) gogBuilds.Add(gogBuild);
            }

            if (gogBuilds.Count == 0)
            {
                _logger.LogDebug("No GOG builds found");
                return false;
            }

            var hasVersionDefinition = VersionDefinition.All(_currentDb!.Db)
                .Select(version =>
                {
                    var matchingIdCount = gogBuilds.Count(g => version.GogBuildsIds.Contains(g));
                    return (matchingIdCount, version);
                })
                .Where(row => row.matchingIdCount > 0)
                .OrderByDescending(row => row.matchingIdCount)
                .Select(t => t.version)
                .TryGetFirst(out versionDefinition);

            if (!hasVersionDefinition)
            {
                _logger.LogDebug("No matching version definition found");
                return false;
            }
        }
        else if (gameStore == GameStore.Steam)
        {
            List<SteamManifest.ReadOnly> steamManifests = [];
            
            foreach (var steamId in locatorIds)
            {
                if (!ulong.TryParse(steamId.Value, out var parsedId))
                {
                    _logger.LogDebug("Steam locator {Raw} metadata is not a valid ulong", steamId);
                    return false;
                }

                var hasManifest = SteamManifest.FindByManifestId(Current, ManifestId.From(parsedId)).TryGetFirst(out var steamManifest);
                if (hasManifest) steamManifests.Add(steamManifest);
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
        else if (gameStore == GameStore.EGS)
        {
            var versionsByManifestHash = VersionDefinition.All(_currentDb!.Db)
                .SelectMany(version =>
                    {
                        if (VersionDefinition.EpicGameStoreBuildsIds.IsIn(version)) 
                            return version.EpicGameStoreBuilds.Select(build => (Version: version, Build: build));
                        return [];
                    }
                )
                .ToLookup(row => row.Build.ManifestHash);

            var builds = locatorIds
                .Select(locatorString => ManifestHash.FromUnsanitized(locatorString.Value))
                .SelectMany(manifestHash => versionsByManifestHash[manifestHash].Select(row => (row.Version, manifestHash)));

            if (builds.TryGetFirst(out var build))
            {
                versionDefinition = build.Version;
                return true;
            }

            return false;
        }
        else
        {
            _logger.LogDebug("No way to get game version for: {Store}", gameStore);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool TryGetLocatorIdsForVanityVersion(GameStore gameStore, VanityVersion version, out LocatorId[] commonIds)
    {
        if (!VersionDefinition.FindByName(Current, version.Value).TryGetFirst(out var versionDef))
        {
            commonIds = [];
            return false;
        }

        commonIds = GetLocatorIdsForVersionDefinition(gameStore, versionDef);
        return true;
    }

    public LocatorId[] GetLocatorIdsForVersionDefinition(GameStore gameStore, VersionDefinition.ReadOnly versionDefinition)
    {
        if (gameStore == GameStore.GOG)
        {
            return versionDefinition.GogBuilds.Select(build => LocatorId.From(build.BuildId.ToString())).ToArray();
        }

        if (gameStore == GameStore.Steam)
        {
            return versionDefinition.SteamManifests.Select(manifest => LocatorId.From(manifest.ManifestId.ToString())).ToArray();
        }
        
        if (gameStore == GameStore.EGS)
        {
            return versionDefinition.EpicGameStoreBuilds.Select(build => LocatorId.From(build.ManifestHash.Value)).ToArray();
        }

        throw new NotSupportedException("No way to get common IDs for: " + gameStore);
    }

    /// <inheritdoc />
    public Optional<VersionData> SuggestVersionData(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files)
    {
        var filesSet = files.ToHashSet();

        List<(VersionData VersionData, int Matches)> versionMatches = [];
        foreach (var versionDefinition in GetVersionDefinitions(gameInstallation.Game.GameId))
        {
            var locatorIds = GetLocatorIdsForVersionDefinition(gameInstallation.Store, versionDefinition);

            var matchingCount = GetGameFiles((gameInstallation.Store, locatorIds))
                .Count(file => filesSet.Contains((file.Path, file.Hash)));

            versionMatches.Add((new VersionData(locatorIds, VanityVersion.From(versionDefinition.Name)), matchingCount));
        }

        return versionMatches
            .OrderByDescending(t => t.Matches)
            .Select(t => t.VersionData)
            .FirstOrOptional(_ => true);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var connection in _databases.Values)
        {
            connection.Backend.Dispose();
            connection.Store.Dispose();
        }
    }
}
