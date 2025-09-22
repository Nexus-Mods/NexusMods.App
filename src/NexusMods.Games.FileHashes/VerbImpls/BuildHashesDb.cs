using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.EpicGameStore.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Sdk.Hashes;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Games.FileHashes.DTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.Networking.EpicGameStore.DTOs.EgData;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk;
using NexusMods.Sdk.ProxyConsole;
using YamlDotNet.Serialization;
using Build = NexusMods.Networking.EpicGameStore.DTOs.EgData.Build;
using BuildId = NexusMods.Abstractions.GOG.Values.BuildId;
using Manifest = NexusMods.Abstractions.Steam.DTOs.Manifest;
using OperatingSystem = NexusMods.Abstractions.Games.FileHashes.Values.OperatingSystem;

namespace NexusMods.Games.FileHashes.VerbImpls;

// Format is a list of (Game, (OS, (Version, VersionDefinition)))
using VersionFileFormat = Dictionary<string, Dictionary<string, VersionContribDefinition[]>>;

public class BuildHashesDb : IAsyncDisposable
{
    private readonly TemporaryPath _tempFolder;
    private readonly DatomStore _datomStore;
    private readonly Connection _connection;
    private readonly IRenderer _renderer;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private readonly Dictionary<(RelativePath Path, EntityId HashId), EntityId> _knownHashPaths = new();
    private readonly MnemonicDB.Storage.RocksDbBackend.Backend _backend;
    private readonly IGameRegistry _gameRegistry;

    public BuildHashesDb(IRenderer renderer, IServiceProvider provider, TemporaryFileManager temporaryFileManager, IGameRegistry gameRegistry)
    {
        Provider = provider;
        _renderer = renderer;
        _gameRegistry = gameRegistry;
        _tempFolder = temporaryFileManager.CreateFolder();
        _jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();
        
        
        
        _backend = new MnemonicDB.Storage.RocksDbBackend.Backend();
        var settings = new DatomStoreSettings
        {
            Path = _tempFolder,
        };
        _datomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), settings, _backend);
        _connection = new Connection(provider.GetRequiredService<ILogger<Connection>>(), _datomStore, provider, []);
    }
    
    public IServiceProvider Provider { get; set; }

    public async Task BuildFrom(AbsolutePath path, AbsolutePath output)
    {
        try
        {
            await AddHashes(path);
            await AddGogData(path);
            await AddSteamData(path);
            await AddEpicGameStoreData(path);
            await AddVersions(path);
        }
        catch (Exception ex)
        {
            await _renderer.Error(ex, "Failed to build database");
            throw;
        }

        await _renderer.TextLine("Exporting database");
        await _connection.FlushAndCompact();
        
        _datomStore.Dispose();
        _backend.Dispose();
        
        if (output.FileExists)
            output.Delete();
        
        var hashesDbPath = output / "game_hashes_db.zip";
        if (hashesDbPath.FileExists)
            hashesDbPath.Delete();
        
        var manifestPath = output / "manifest.json";
        ZipFile.CreateFromDirectory(_tempFolder.Path.ToString(), hashesDbPath.ToString(), CompressionLevel.SmallestSize, false);

        var zipHash = await hashesDbPath.XxHash3Async();

        var manifest = new DTOs.Manifest()
        {
            CreatedAt = DateTimeOffset.UtcNow,
            Assets =
            [
                new DTOs.Asset()
                {
                    Type = AssetType.GameHashDb,
                    Hash = zipHash,
                    Size = hashesDbPath.FileInfo.Size,
                },
            ],
        };

        await using var manifestStream = manifestPath.Create();
        await JsonSerializer.SerializeAsync(manifestStream, manifest, _jsonOptions);

        await _renderer.TextLine("Database built and exported to {0}. Final size {1}. Hash: {2}", output, hashesDbPath.FileInfo.Size, zipHash);
    }

    private async Task AddVersions(AbsolutePath path)
    {
        var versionsPath = path / "contrib/version_aliases.yaml";
        await using var versionsStream = versionsPath.Read();
        var versions = new DeserializerBuilder()
            .Build()
            .Deserialize<VersionFileFormat>(new StreamReader(versionsStream));

        var versionData = (from game in versions
            let gameName = game.Key
            from os in game.Value
            let osName = os.Key
            from definition in os.Value
            select (gameName, osName, definition)).ToArray();
        
        await _renderer.TextLine("Found {0} version mappings", versionData.Length);
        
        var referenceDb = _connection.Db;
        using var tx = _connection.BeginTransaction();
        foreach (var (gameName, osName, definition) in versionData)
        {
            var gameObject = _gameRegistry.SupportedGames.First(g => g.Name == gameName);
            
            var os = osName switch
            {
                "Windows" => OperatingSystem.Windows,
                "MacOS" => OperatingSystem.MacOS,
                "Linux" => OperatingSystem.Linux,
                _ => throw new Exception("Unknown OS"),
            };

            var versionDef = new VersionDefinition.New(tx)
            {
                Name = definition.Name,
                OperatingSystem = os,
                GameId = gameObject.GameId,
                GOG = definition.GOG ?? [],
                Steam = definition.Steam ?? [],
                EpicBuildIds = definition.Epic ?? [],
            };

            var productIds = ((IGogGame)gameObject).GogIds.Select(id => ProductId.From((ulong)id));

            // ?? is needed here because the parser 
            foreach (var id in definition.GOG ?? [])
            {
                try
                {
                    var build = GogBuild
                        .FindByVersionName(referenceDb, id)
                        .Where(g => g.OperatingSystem == os)
                        .Single(g => productIds.Contains(g.ProductId));
                    tx.Add(versionDef, VersionDefinition.GogBuildsIds, build.Id);
                }
                catch (InvalidOperationException _)
                {
                    await _renderer.TextLine("Failed to find GOG build for {0} {1} {2}", gameName, osName, id);
                }
            }

            foreach (var id in definition.Steam ?? [])
            {
                try
                {
                    var manifest = SteamManifest
                        .FindByManifestId(referenceDb, ManifestId.From(ulong.Parse(id)))
                        .Single();
                    tx.Add(versionDef, VersionDefinition.SteamManifestsIds, manifest.Id);
                }
                catch (InvalidOperationException _)
                {
                    await _renderer.TextLine("Failed to anchor Steam manifest {0} to {1} for {2}", id, definition.Name, gameName);
                }
            }
            
            foreach (var id in definition.Epic ?? [])
            {
                try
                {
                    var manifest = EpicGameStoreBuild
                        .FindByManifestHash(referenceDb, ManifestHash.FromUnsanitized(id))
                        .Single();
                    tx.Add(versionDef, VersionDefinition.EpicGameStoreBuilds, manifest.Id);
                }
                catch (InvalidOperationException _)
                {
                    await _renderer.TextLine("Failed to find Epic manifest for {0} {1} {2}", gameName, osName, id);
                }
            }
        }

        await tx.Commit();
    }

    private async Task AddSteamData(AbsolutePath path)
    {
        var refDb = _connection.Db;
        using var tx = _connection.BeginTransaction();
        await _renderer.TextLine("Importing Steam data");
        
        var appPath = path / "json" / "stores" / "steam" / "apps";

        var manifestCount = 0;
        var pathCounts = 0;
        
        var manifests = await LoadAllFromFolder<Manifest>(path / "json" / "stores" / "steam" / "manifests");
        
        var apps = await LoadAllFromFolder<ProductInfo>(path / "json" / "stores" / "steam" / "apps");
        
        var depotsToAppId = (from app in apps.Values
            from depot in app.Depots
            select (depot.DepotId, app.AppId))
            .ToLookup(t => t.DepotId, t => t.AppId);
        

        foreach (var (manifestName, manifestInfo) in manifests)
        {
            var manifestPath = path / "json" / "stores" / "steam" / "manifests" / (manifestInfo.ManifestId + ".json");
            await using var manifestStream = manifestPath.Read();
            var parsedManifest = (await JsonSerializer.DeserializeAsync<Manifest>(manifestStream, _jsonOptions))!;

            var pathIds = new List<EntityId>();
            bool hasFailures = false;
            foreach (var file in parsedManifest.Files)
            {
                // Steam manifests include folders, which we don't care about
                if (file.Chunks.Length == 0)
                    continue;

                var refDbRelation = HashRelation.FindBySha1(refDb, file.Hash).FirstOrDefault();
                if (!refDbRelation.IsValid())
                {
                    await _renderer.TextLine($"Skipping {0} due to missing SHA1 hashes", manifestInfo.ManifestId);
                    hasFailures = true;
                    break;
                }

                // The paths from steam can be in a format that isn't directly valid, so we sanitize them
                var sanitizedPath = RelativePath.FromUnsanitizedInput(file.Path.ToString());
                var relationId = EnsureHashPathRelation(tx, refDb, sanitizedPath,
                    file.Hash
                );
                pathIds.Add(relationId);
                pathCounts++;
            }

            if (hasFailures)
            {
                continue;
            }
            
            if (!depotsToAppId[manifestInfo.DepotId].TryGetFirst(out var appId))
            {
                await _renderer.TextLine("Skipping Manifest {0} because it doesn't have a matching AppId", manifestInfo.ManifestId);
                continue;
            }

            _ = new SteamManifest.New(tx)
            {
                AppId = appId,
                DepotId = manifestInfo.DepotId,
                ManifestId = manifestInfo.ManifestId,
                Name = manifestName,
                FilesIds = pathIds,
            };
            manifestCount++;
        }

        var result = await tx.Commit();
        RemapHashPaths(result);
        await _renderer.TextLine("Imported {0} manifests with {1} paths", manifestCount, pathCounts);
    }

    private async Task AddGogData(AbsolutePath path)
    {
        using var tx = _connection.BeginTransaction();
        await _renderer.TextLine("Importing GOG data");
        
        var foundHashesPath = path / "json" / "stores" / "gog" / "found_hashes";
        
        var buildDetails = await LoadAllFromFolder<BuildDetails>(path / "json" / "stores" / "gog" / "build_details");
        var foundHashes = await LoadAllFromFolder<Dictionary<string, Hash>>(path / "json" / "stores" / "gog" / "found_hashes");
        var builds = await LoadAllFromFolder<NexusMods.Abstractions.GOG.DTOs.Build>(path / "json" / "stores" / "gog" / "builds");

        var refDb = _connection.Db;
        var pathCount = 0;
        var buildCount = 0;
        
        Dictionary<string, EntityId> manifestEntities = new();
        Dictionary<string, List<EntityId>> pathEntities = new(); 
        
        // First we insert all the manifests, we do this by using the manifestId from the filename
        // and inserting all the hash/path pairs
        foreach (var (manifestId, files) in foundHashes)
        {
            List<EntityId> pathIds = new();
            foreach (var (relPath, hash) in files)
            {
                var refDbRelation = HashRelation.FindByXxHash3(refDb, hash).FirstOrDefault();
                if (!refDbRelation.IsValid())
                {
                    throw new Exception("Hash not found in the reference database for path " + relPath + " and hash " + hash);
                }

                var relationId = EnsureHashPathRelation(tx, refDb, relPath, hash);
                pathIds.Add(relationId);
                pathCount++;
            }
            pathEntities[manifestId] = pathIds;

            var manifestEnt = new GogManifest.New(tx)
            {
                ManifestId = manifestId,
                FilesIds = pathIds,
            };
            manifestEntities[manifestId] = manifestEnt.Id;
        }
        
        foreach (var (buildId, build) in builds)
        {
            try
            {
                var buildDetail = buildDetails[buildId];
                var primaryDepot = buildDetail.Depots.First();
                var primaryHashPaths = pathEntities[primaryDepot.Manifest];

                var depotIds = new List<EntityId>();

                foreach (var depot in buildDetail.Depots)
                {
                    var depotEnt = new GogDepot.New(tx)
                    {
                        ProductId = depot.ProductId,
                        Size = depot.Size,
                        CompressedSize = depot.CompressedSize,
                        ManifestId = manifestEntities[depot.Manifest],
                        Languages = depot.Languages,
                    };
                    depotIds.Add(depotEnt.Id);
                }
                
                var os = build.OS switch
                {
                    "windows" => OperatingSystem.Windows,
                    "osx" => OperatingSystem.MacOS,
                    "linux" => OperatingSystem.Linux,
                    _ => throw new Exception("Unknown OS"),
                };

                _ = new GogBuild.New(tx)
                {
                    BuildId = BuildId.From(ulong.Parse(buildId)),
                    ProductId = build.ProductId,
                    ManifestId = primaryDepot.Manifest,
                    OperatingSystem = os,
                    VersionName = build.VersionName,
                    Public = build.Public,
                    FilesIds = primaryHashPaths,
                    DepotsIds = depotIds,
                };
            }
            catch (Exception ex)
            {
                await _renderer.Error(ex, "Failed to import {0}: {1}", build.BuildId, ex.Message);
            }
            buildCount++;
        }

        var result = await tx.Commit();
        RemapHashPaths(result);
        
        await _renderer.TextLine("Imported {0} builds with {1} paths", buildCount, pathCount);
    }

    private async Task<Dictionary<string, T>> LoadAllFromFolder<T>(AbsolutePath folder)
    {
        var dict = new Dictionary<string, T>();
        foreach (var file in folder.EnumerateFiles(KnownExtensions.Json))
        {
            try
            {
                await using var fs = file.Read();
                var parsed = JsonSerializer.Deserialize<T>(fs, _jsonOptions)!;
                dict[file.GetFileNameWithoutExtension()] = parsed;
            }
            catch (Exception ex)
            {
                await _renderer.Error(ex, "Failed to load {0}: {1}", file, ex.Message);
            }
        }

        return dict;
    }

    private async Task AddEpicGameStoreData(AbsolutePath path)
    {
        using var tx = _connection.BeginTransaction();
        await _renderer.TextLine("Importing GOG data");

        var buildsPath = path / "json" / "stores" / "egs" / "builds";

        var metadata = new Dictionary<string, Build>();
        var files = new Dictionary<string, BuildFile[]>();

        foreach (var itemFolder in buildsPath.EnumerateDirectories())
        {
            var itemId = itemFolder.GetFileNameWithoutExtension();

            foreach (var file in itemFolder.EnumerateFiles(KnownExtensions.Json))
            {
                await using var fs = file.Read();
                if (file.FileName.EndsWith("_metadata.json"))
                {
                    metadata[itemId] = (await JsonSerializer.DeserializeAsync<Build>(fs, _jsonOptions))!;
                }
                if (file.FileName.EndsWith("_files.json"))
                {
                    files[itemId] = (await JsonSerializer.DeserializeAsync<BuildFile[]>(fs, _jsonOptions))!;
                }
            }
        }

        var buildCount = 0;
        foreach (var (id, build) in metadata)
        {
            try
            {
                var buildFiles = files[id];
                
                var pathIds = new List<EntityId>();

                foreach (var file in buildFiles)
                {
                    var relativePath = RelativePath.FromUnsanitizedInput(file.FileName);
                    var relation = EnsureHashPathRelation(tx, _connection.Db, relativePath, Sha1Value.FromHex(file.FileHash));
                    pathIds.Add(relation);
                }

                _ = new EpicGameStoreBuild.New(tx)
                {
                    BuildId = NexusMods.Abstractions.EpicGameStore.Values.BuildId.FromUnsanitized(build.Id),
                    ManifestHash = ManifestHash.FromUnsanitized(build.ManifestHash),
                    ItemId = ItemId.FromUnsanitized(id),
                    AppName = build.AppName,
                    BuildVersion = build.BuildVersion,
                    LabelName = build.LabelName,
                    CreatedAt = build.CreatedAt,
                    UpdatedAt = build.UpdatedAt,
                    FilesIds = pathIds,
                };
                
                buildCount++;
            }
            catch (Exception ex)
            {
                await _renderer.Error(ex, "Failed to import {0}: {1}", id, ex.Message);
            }
        }
        
        var result = await tx.Commit();
        await _renderer.TextLine("Imported {0} EGS builds", buildCount);
        RemapHashPaths(result);
    }

    private void RemapHashPaths(ICommitResult result)
    {
        var toRemap = _knownHashPaths.Where(f => f.Value.Partition == PartitionId.Temp).ToArray();
        
        foreach (var (key, id) in toRemap)
        {
            _knownHashPaths[key] = result[id];
        }
    }

    private async Task AddHashes(AbsolutePath path)
    {
        var hashPath = path / "json" / "hashes";

        await _renderer.TextLine("Importing hashes");
        using var tx = _connection.BeginTransaction();
        
        var count = 0;
        foreach (var file in hashPath.EnumerateFiles(KnownExtensions.Json))
        {
            try
            {
                await using var fs = file.Read();
                var parsedHash = (await JsonSerializer.DeserializeAsync<MultiHash>(fs, _jsonOptions))!;

                _ = new HashRelation.New(tx)
                {
                    XxHash3 = parsedHash.XxHash3,
                    XxHash64 = parsedHash.XxHash64,
                    MinimalHash = parsedHash.MinimalHash,
                    Md5 = parsedHash.Md5,
                    Sha1 = parsedHash.Sha1,
                    Crc32 = parsedHash.Crc32,
                    Size = parsedHash.Size,
                };
                count++;
            }
            catch (Exception ex)
            {
                await _renderer.Error(ex, "Failed to import {0}: {1}", file, ex.Message);
            }
        }

        await tx.Commit();
        await _renderer.TextLine("Imported {0} hashes", count);
    }

    /// <summary>
    /// Find or insert a hash path relation
    /// </summary>
    private EntityId EnsureHashPathRelation(ITransaction tx, IDb referenceDb, RelativePath path, Hash hash)
    {
        var hashRelation = HashRelation.FindByXxHash3(referenceDb, hash).FirstOrDefault();
        
        if (!hashRelation.IsValid())
        {
            throw new Exception("Hash not found in the reference database");
        }
        
        return EnsureHashPathRelation(tx, path, hashRelation);
    }
    
    /// <summary>
    /// Find or insert a hash path relation
    /// </summary>
    private EntityId EnsureHashPathRelation(ITransaction tx, IDb referenceDb, RelativePath path, Sha1Value hash)
    {
        var hashRelation = HashRelation.FindBySha1(referenceDb, hash).FirstOrDefault();
        
        if (!hashRelation.IsValid())
        {
            throw new Exception("Hash not found in the reference database");
        }
        
        return EnsureHashPathRelation(tx, path, hashRelation);
    }

    private EntityId EnsureHashPathRelation(ITransaction tx, RelativePath path, HashRelationId hashRelation)
    {
        if (_knownHashPaths.TryGetValue((path, hashRelation), out var entityId))
        {
            return entityId;
        }
        
        var newHashPathRelation = new PathHashRelation.New(tx)
        {
            Path = path,
            HashId = hashRelation,
        };
        
        _knownHashPaths[(path, hashRelation)] = newHashPathRelation.Id;
        
        return newHashPathRelation.Id;
    }

    public async ValueTask DisposeAsync()
    {
        _datomStore.Dispose();
        await _tempFolder.DisposeAsync();
    }
}
