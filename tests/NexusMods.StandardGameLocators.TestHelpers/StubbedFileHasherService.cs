using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Hashes;
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

    public StubbedFileHasherService(IServiceProvider provider)
    {
        _provider = provider;
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
        
        
        foreach (var entry in zipArchive.Entries)
        {
            await using var stream = entry.Open();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var hasher = new MultiHasher();
            var result = await hasher.HashStream(memoryStream);

            var relation = new HashRelation.New(tx)
            {
                MinimalHash = result.MinimalHash,
                XxHash3 = result.XxHash3,
                Sha1 = result.Sha1,
                XxHash64 = result.XxHash64,
                Md5 = result.Md5,
                Size = result.Size,
                Crc32 = result.Crc32,
            };

            var path = new PathHashRelation.New(tx)
            {
                Path = entry.FullName,
                HashId = relation,
            };
        }
        
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
}
