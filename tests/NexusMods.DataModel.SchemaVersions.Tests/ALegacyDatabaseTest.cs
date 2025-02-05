using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Loadouts;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.Abstractions;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;

namespace NexusMods.DataModel.SchemaVersions.Tests;

public abstract class ALegacyDatabaseTest
{
    private readonly SignatureChecker _zipSignatureChecker = new(FileType.ZIP);
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IFileExtractor Extractor;

    protected ALegacyDatabaseTest(IServiceProvider provider)
    {
        ServiceProvider = provider;
        Extractor = provider.GetRequiredService<IFileExtractor>();
    }

    public record TempConnection : IDisposable
    {
        public required IStoreBackend Backend { get; init; }
        public required TemporaryFileManager TemporaryFileManager { get; init; }
        public required DatomStore DatomStore { get; init; }
        
        public required IConnection Connection { get; init; }
        public required MigrationId OldId { get; init; }
        
        public void Dispose()
        {
            DatomStore.Dispose();
            Backend.Dispose();
            TemporaryFileManager.Dispose();
        }
    }
    
    [MustDisposeResource]
    public async Task<TempConnection> ConnectionFor(string name)
    {
        var path = DatabaseFolder().Combine(name); 
        path.FileExists.Should().BeTrue("the database file should exist");
        
        await using (var stream = path.Read())
        {
            var isZip = await _zipSignatureChecker.MatchesAnyAsync(stream);
            isZip.Should().BeTrue("the database file should be a ZIP archive, you may need to pull the file from LFS (`git lfs pull`)");
        }

        var basePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("Temp").Combine($"Test-{Guid.NewGuid()}");
        basePath.CreateDirectory();
        basePath.DirectoryExists().Should().BeTrue();

        var temporaryFileManager = new TemporaryFileManager(FileSystem.Shared, basePath: basePath);

        var workingFolder = temporaryFileManager.CreateFolder();
        await Extractor.ExtractAllAsync(path, workingFolder.Path);

        var datamodelFolder = workingFolder.Path.Combine("MnemonicDB.rocksdb");
        datamodelFolder.DirectoryExists().Should().BeTrue("the extracted database folder should exist");
        datamodelFolder.EnumerateFiles().Should().NotBeEmpty("the extracted database folder should contain files");

        var backend = new Backend();
        var settings = new DatomStoreSettings
        {
            Path = datamodelFolder,
        };
        var datomStore = new DatomStore(ServiceProvider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        var connection = new Connection(ServiceProvider.GetRequiredService<ILogger<Connection>>(), datomStore, ServiceProvider, ServiceProvider.GetServices<IAnalyzer>());
        
        var oldMigrationId = RecordedVersion(connection.Db);
        
        var migrationService = new MigrationService(ServiceProvider.GetRequiredService<ILogger<MigrationService>>(), connection, ServiceProvider, ServiceProvider.GetServices<MigrationDefinition>());
        await migrationService.MigrateAll();

        return new TempConnection()
        {
            OldId = oldMigrationId,
            Connection = connection,
            DatomStore = datomStore,
            Backend = backend,
            TemporaryFileManager = temporaryFileManager
        };
    }

    private MigrationId RecordedVersion(IDb db)
    {
        var cache = db.AttributeCache;
        if (!cache.Has(SchemaVersion.CurrentVersion.Id))
            return MigrationId.From(0);
        
        var fingerprints = db.Datoms(SchemaVersion.CurrentVersion);
        if (fingerprints.Count == 0)
            return MigrationId.From(0);
        return (MigrationId)db.Datoms(SchemaVersion.CurrentVersion).Single().Resolved(db.Connection.AttributeResolver).ObjectValue;
    }

    private Statistics GetStatistics(IDb db, string name, MigrationId oldId)
    {
        var timestampAttr = MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp;
        
        var timestamp = (DateTimeOffset)db.Get(PartitionId.Transactions.MakeEntityId(1)).Resolved(db.Connection).First(t => t.A == timestampAttr).ObjectValue;
        
        return new Statistics
        {
            Name = name,
            OldId = oldId.Value,
            NewId = RecordedVersion(db).Value,
            Loadouts = Loadout.All(db).Count,
            LoadoutItemGroups = LoadoutItemGroup.All(db).Count,
            Files = LoadoutItemWithTargetPath.All(db).Count,
            Collections = CollectionGroup.All(db).Count,
            Created = timestamp.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Statistics about the data in a database
    /// </summary>
    record Statistics
    {
        public string Name { get; init; }
        
        public ushort OldId { get; init; }
        
        public ushort NewId{ get; init; }
        
        public int Loadouts { get; init; }
        public int LoadoutItemGroups { get; init; }
        public int Files { get; init; }
        public int Collections { get; init; }
        public string Created { get; init; }
    }
    

    public static IEnumerable<object[]> DatabaseNames()
    {
        var databaseFolder = DatabaseFolder();
        foreach (var file in databaseFolder.EnumerateFiles("*.zip").OrderBy(f => f.ToString()))
        {
            yield return [file.Name];
        }
    }

    protected static AbsolutePath DatabaseFolder()
    {
        var basePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Parent.Parent.Parent;
        var databaseFolder = basePath.Combine("Resources/Databases");
        return databaseFolder;
    }
}
