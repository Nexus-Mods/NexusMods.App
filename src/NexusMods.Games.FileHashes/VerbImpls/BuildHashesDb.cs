using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Extensions.Hashing;
using NexusMods.Games.FileHashes.DTOs;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.ProxyConsole.Abstractions;
using Manifest = NexusMods.Abstractions.Steam.DTOs.Manifest;
using OperatingSystem = NexusMods.Abstractions.Games.FileHashes.Values.OperatingSystem;

namespace NexusMods.Games.FileHashes.VerbImpls;

public class BuildHashesDb : IAsyncDisposable
{
    private readonly TemporaryPath _tempFolder;
    private readonly DatomStore _datomStore;
    private readonly Connection _connection;
    private readonly IRenderer _renderer;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private readonly Dictionary<(RelativePath Path, EntityId HashId), EntityId> _knownHashPaths = new();
    private readonly Backend _backend;

    public BuildHashesDb(IRenderer renderer, IServiceProvider provider, TemporaryFileManager temporaryFileManager)
    {
        Provider = provider;
        _renderer = renderer;
        _tempFolder = temporaryFileManager.CreateFolder();
        _jsonOptions = provider.GetRequiredService<JsonSerializerOptions>();
        
        
        
        _backend = new Backend();
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
        await AddHashes(path);
        await AddGogData(path);
        await AddSteamData(path);
        
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

        var zipHash = await hashesDbPath.XxHash64Async();

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

    private async Task AddSteamData(AbsolutePath path)
    {
        var refDb = _connection.Db;
        using var tx = _connection.BeginTransaction();
        await _renderer.TextLine("Importing Steam data");
        
        var appPath = path / "json" / "stores" / "steam" / "apps";

        var manifestCount = 0;
        var pathCounts = 0;
        
        foreach (var appData in appPath.EnumerateFiles(KnownExtensions.Json))
        {
            await using var appStream = appData.Read();
            var parsedAppData = (await JsonSerializer.DeserializeAsync<ProductInfo>(appStream, _jsonOptions))!;

            foreach (var depot in parsedAppData.Depots)
            {
                foreach (var (manifestName, manifestInfo) in depot.Manifests)
                {
                    var manifestPath = path / "json" / "stores" / "steam" / "manifests" / (manifestInfo.ManifestId + ".json");
                    await using var manifestStream = manifestPath.Read();
                    var parsedManifest = (await JsonSerializer.DeserializeAsync<Manifest>(manifestStream, _jsonOptions))!;

                    var pathIds = new List<EntityId>();
                    foreach (var file in parsedManifest.Files)
                    {
                        // Steam manifests include folders, which we don't care about
                        if (file.Chunks.Length == 0)
                            continue;
                        
                        var refDbRelation = HashRelation.FindBySha1(refDb, file.Hash).FirstOrDefault();
                        if (!refDbRelation.IsValid())
                        {
                            throw new Exception($"Sha1 not found in the reference database for path {file.Path} and hash {file.Hash}");
                        }

                        var relationId = EnsureHashPathRelation(tx, refDb, file.Path, file.Hash);
                        pathIds.Add(relationId);
                        pathCounts++;
                    }
                    
                    _ = new SteamManifest.New(tx)
                    {
                        AppId = parsedAppData.AppId,
                        DepotId = depot.DepotId,
                        ManifestId = manifestInfo.ManifestId,
                        Name = manifestName,
                        FilesIds = pathIds,
                    };
                    manifestCount++;
                }
            }
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

        var refDb = _connection.Db;
        var pathCount = 0;
        var buildCount = 0;
        foreach (var foundHashesFile in foundHashesPath.EnumerateFiles(KnownExtensions.Json))
        {
            try
            {
                await using var fs = foundHashesFile.Read();
                var parsedFoundHashes = (await JsonSerializer.DeserializeAsync<Dictionary<string, Hash>>(fs, _jsonOptions))!;

                var buildIdString = foundHashesFile.GetFileNameWithoutExtension();
                var buildId = BuildId.From(ulong.Parse(buildIdString));


                var pathIds = new List<EntityId>();

                foreach (var (relativePath, hash) in parsedFoundHashes)
                {
                    var refDbRelation = HashRelation.FindByXxHash3(refDb, Hash.From(hash.Value)).FirstOrDefault();
                    if (!refDbRelation.IsValid())
                    {
                        throw new Exception("Hash not found in the reference database for path " + relativePath + " and hash " + hash);
                    }

                    var relationId = EnsureHashPathRelation(tx, refDb, relativePath, hash);
                    pathIds.Add(relationId);
                    pathCount++;
                }

                var buildPath = path / "json" / "stores"/ "gog" / "builds" / (buildId + ".json");
                await using var buildFs = buildPath.Read();
                var parsedBuild = (await JsonSerializer.DeserializeAsync<NexusMods.Abstractions.GOG.DTOs.Build>(buildFs, _jsonOptions))!;

                var os = parsedBuild.OS switch
                {
                    "windows" => OperatingSystem.Windows,
                    "osx" => OperatingSystem.MacOS,
                    "linux" => OperatingSystem.Linux,
                    _ => throw new Exception("Unknown OS"),
                };

                _ = new GogBuild.New(tx)
                {
                    BuildId = buildId,
                    ProductId = parsedBuild.ProductId,
                    OperatingSystem = os,
                    VersionName = parsedBuild.VersionName,
                    Public = parsedBuild.Public,
                    FilesIds = pathIds,
                };
            }
            catch (Exception ex)
            {
                await _renderer.Error(ex, "Failed to import {0}: {1}", foundHashesFile, ex.Message);
            }
            buildCount++;
        }

        var result = await tx.Commit();
        RemapHashPaths(result);
        
        await _renderer.TextLine("Imported {0} builds with {1} paths", buildCount, pathCount);
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
    private EntityId EnsureHashPathRelation(ITransaction tx, IDb referenceDb, RelativePath path, Sha1 hash)
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
