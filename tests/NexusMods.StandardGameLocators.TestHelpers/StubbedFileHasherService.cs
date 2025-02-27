using System.IO.Compression;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Games.FileHashes.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.InMemoryBackend;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class StubbedFileHasherService : IFileHashesService
{
    private readonly IServiceProvider _provider;
    private DatomStore _datomStore;
    private Connection _connection;
    private IDb? _current;
    private readonly IFileStore _fileStore;
    private readonly TemporaryFileManager _temporaryFileManager;
    private readonly Dictionary<LocatorId, EntityId[]> _versionFiles;

    public StubbedFileHasherService(IServiceProvider provider, IFileStore fileStore, TemporaryFileManager temporaryFileManager)
    {
        _provider = provider;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
        _versionFiles = new Dictionary<LocatorId, EntityId[]>();
    }

    private async Task SetupDb()
    {
        var backend = new Backend();
        var settings = new DatomStoreSettings()
        {
            Path = default(AbsolutePath),
        };
        _datomStore = new DatomStore(_provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        _connection = new Connection(_provider.GetRequiredService<ILogger<Connection>>(), _datomStore, _provider, []);


        foreach (var file in new[] {"StubbedGameState.zip", "StubbedGameState_game_v2.zip"})
        {
            var stateFile = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory) / "Resources" / file;
            using var zipArchive = new ZipArchive(stateFile.Read());

            using var tx = _connection.BeginTransaction();

            List<ArchivedFileEntry> archiveFiles = [];
            List<EntityId> pathIds = [];
            
            foreach (var entry in zipArchive.Entries)
            {
                if (!entry.FullName.StartsWith("game"))
                    continue;

                var relativePath = RelativePath.FromUnsanitizedInput(string.Join("/", RelativePath.FromUnsanitizedInput(entry.FullName).Parts.Skip(1)));
                var gamePath = new GamePath(LocationId.Game, entry.FullName);
                
                if (entry.Length == 0)
                    continue;

                await using var stream = entry.Open();
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var hasher = new MultiHasher();
                var hashResult = await hasher.HashStream(memoryStream);

                memoryStream.Position = 0;
                archiveFiles.Add(new ArchivedFileEntry(new MemoryStreamFactory(RelativePath.FromUnsanitizedInput(entry.FullName), memoryStream), hashResult.XxHash3, hashResult.Size));

                var relation = new HashRelation.New(tx)
                {
                    MinimalHash = hashResult.MinimalHash,
                    XxHash3 = hashResult.XxHash3,
                    Sha1 = hashResult.Sha1,
                    XxHash64 = hashResult.XxHash64,
                    Md5 = hashResult.Md5,
                    Size = hashResult.Size,
                    Crc32 = hashResult.Crc32,
                };
                

                var path = new PathHashRelation.New(tx)
                {
                    Path = relativePath,
                    HashId = relation,
                };
                pathIds.Add(path.Id);
            }

            await _fileStore.BackupFiles(archiveFiles);
            var results = await tx.Commit();

            _versionFiles[LocatorId.From(file)] = pathIds.Select(id => results[id]).ToArray();
            _current = results.Db;
        }
    }

    public Task CheckForUpdate(bool forceUpdate = false)
    {
        return Task.CompletedTask;
    }

    public IEnumerable<VanityVersion> GetKnownVanityVersions(GameId gameId)
    {
        return [VanityVersion.From("1.0.Stubbed"), VanityVersion.From("1.1.Stubbed")];
    }

    public async ValueTask<IDb> GetFileHashesDb()
    {
        if (_current is not null)
            return _current;
        await SetupDb();
        return _current!;
    }

    public IEnumerable<GameFileRecord> GetGameFiles(LocatorIdsWithGameStore locatorIdsWithGameStore)
    {
        var (_, locatorIds) = locatorIdsWithGameStore;

        var firstLocatorId = locatorIds.First();
        if (!_versionFiles.TryGetValue(firstLocatorId, out var fileIds))
        {
            if (firstLocatorId == "3976631895")
                fileIds = _versionFiles[LocatorId.From("StubbedGameState.zip")];
        }
        
        foreach (var fileId in fileIds!)
        {
            var file = PathHashRelation.Load(Current, fileId);
            yield return new GameFileRecord
            {
                Path = new GamePath(LocationId.Game, file.Path),
                Size = file.Hash.Size,
                MinimalHash = file.Hash.MinimalHash,
                Hash = file.Hash.XxHash3,
            };
        }
    }

    public IDb Current => _current!;

    public bool TryGetVanityVersion(LocatorIdsWithGameStore locatorIdsWithGameStore, out VanityVersion version)
    {
        var (_, locatorIds) = locatorIdsWithGameStore;

        var locatorId = locatorIds.First();
        if (locatorId == "StubbedGameState.zip")
        {
            version = VanityVersion.From("1.0.Stubbed");
            return true;
        }

        if (locatorId == "StubbedGameState_game_v2.zip")
        {
            version = VanityVersion.From("1.1.Stubbed");
            return true;
        }

        // The stubbed Steam tests use this unit as the locator metadata
        if (locatorId == "3976631895")
        {
            version = VanityVersion.From("1.0.Stubbed");
            return true;
        }

        throw new NotSupportedException($"Unknown locator metadata: {locatorId}");
    }

    public LocatorId[] GetLocatorIdsForVersionDefinition(GameStore gameStore, VersionDefinition.ReadOnly versionDefinition) => [];

    public bool TryGetLocatorIdsForVanityVersion(GameStore gameStore, VanityVersion version, out LocatorId[] commonIds)
    {
        switch (version.Value)
        {
            case "1.0.Stubbed":
                commonIds = [LocatorId.From("StubbedGameState.zip")];
                return true;
            case "1.1.Stubbed":
                commonIds = [LocatorId.From("StubbedGameState_game_v2.zip")];
                return true;
            default:
                commonIds = [];
                return false;
        }
    }

    public Optional<VersionData> SuggestVersionData(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files)
    {
        var filesSet = files.ToHashSet();

        List<(VersionData VersionData, int Matches)> versionMatches = [];
        foreach (var versionDefinition in _versionFiles)
        {
            var (locatorIds , _) = versionDefinition;
            var matchingCount = GetGameFiles((gameInstallation.Store, [locatorIds]))
                .Count(file => filesSet.Contains((file.Path, file.Hash)));

            if (!TryGetVanityVersion((gameInstallation.Store, [locatorIds]), out var version))
            {
                throw new Exception("Failed to suggest a game version");
            }
            
            versionMatches.Add((new VersionData([locatorIds], version), matchingCount));
        }

        return versionMatches
            .OrderByDescending(t => t.Matches)
            .Select(t => t.VersionData)
            .FirstOrOptional(_ => true);
    }
}
