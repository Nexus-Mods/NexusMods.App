using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
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

    public StubbedFileHasherService(IServiceProvider provider, IFileStore fileStore, TemporaryFileManager temporaryFileManager)
    {
        _provider = provider;
        _fileStore = fileStore;
        _temporaryFileManager = temporaryFileManager;
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
        
        var stateFile = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Resources/StubbedGameState.zip");
        var zipArchive = new ZipArchive(stateFile.Read());
        
        using var tx = _connection.BeginTransaction();
        
        List<ArchivedFileEntry> archiveFiles = [];
        
        foreach (var entry in zipArchive.Entries)
        {
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
                Path = entry.FullName,
                HashId = relation,
            };
        }

        await _fileStore.BackupFiles(archiveFiles);
        
        var results = await tx.Commit();
        _current = results.Db;
    }

    public Task CheckForUpdate(bool forceUpdate = false)
    {
        return Task.CompletedTask;
    }

    public async ValueTask<IDb> GetFileHashesDb()
    {
        await SetupDb();
        return _current!;
    }

    public IEnumerable<GameFileRecord> GetGameFiles(IDb referenceDb, GameInstallation installation, string[] commonIds)
    {
        foreach (var file in PathHashRelation.All(referenceDb))
        {
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
    public string GetGameVersion(GameInstallation installation, IEnumerable<string> locatorMetadata)
    {
        var firstMetadata = locatorMetadata.First();
        if (firstMetadata == "Unknown")
            return "StubbedVersion";
        throw new NotSupportedException($"Unknown locator metadata: {firstMetadata}");
    }
}
