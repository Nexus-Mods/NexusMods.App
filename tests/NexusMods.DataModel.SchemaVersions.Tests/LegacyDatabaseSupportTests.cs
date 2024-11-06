using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;
using Xunit;

namespace NexusMods.DataModel.Migrations.Tests;

public class LegacyDatabaseSupportTests(IServiceProvider provider, TemporaryFileManager tempManager, IFileExtractor extractor)
{
    [Theory]
    [MemberData(nameof(DatabaseNames))]
    public async Task TestDatabase(string name)
    {
        var path = DatabaseFolder().Combine(name);
        path.FileExists.Should().BeTrue();

        await using var workingFolder = tempManager.CreateFolder();
        await extractor.ExtractAllAsync(path, workingFolder.Path);

        using var backend = new Backend();
        var settings = new DatomStoreSettings
        {
            Path = workingFolder.Path.Combine("MnemonicDB.rocksdb"),
        };
        using var datomStore = new DatomStore(provider.GetRequiredService<ILogger<DatomStore>>(), settings, backend);
        var connection = new Connection(provider.GetRequiredService<ILogger<Connection>>(), datomStore, provider, provider.GetServices<IAnalyzer>());
        
        var oldFingerprint = RecordedFingerprint(connection.Db);
        
        var migrationService = new MigrationService(provider.GetRequiredService<ILogger<MigrationService>>(), connection, provider.GetServices<IMigration>());
        await migrationService.Run();
        
        await Verify(GetStatistics(connection.Db, name, oldFingerprint)).UseParameters(name);
    }

    private Hash? RecordedFingerprint(IDb db)
    {
        var cache = db.AttributeCache;
        if (!cache.Has(SchemaVersion.Fingerprint.Id))
            return null;
        
        var fingerprints = db.Datoms(SchemaVersion.Fingerprint);
        if (fingerprints.Count == 0)
            return null;
        return Hash.From(ValueTag.UInt64.Read<ulong>(fingerprints.First().ValueSpan));
    }

    private Statistics GetStatistics(IDb db, string name, Hash? oldFingerprint)
    {
        var timestampAttr = MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp;
        return new Statistics
        {
            Name = name,
            OldFingerprint = oldFingerprint?.ToString() ?? "None",
            NewFingerprint = RecordedFingerprint(db)?.ToString() ?? "None",
            Loadouts = Loadout.All(db).Count,
            LoadoutItemGroups = LoadoutItemGroup.All(db).Count,
            Files = LoadoutItemWithTargetPath.All(db).Count,
            Collections = CollectionGroup.All(db).Count,
            Created = db.Get(PartitionId.Transactions.MakeEntityId(1)).Resolved(db.Connection).First(t => t.A == timestampAttr).ObjectValue.ToString(),
        };
    }

    /// <summary>
    /// Statistics about the data in a database
    /// </summary>
    record Statistics
    {
        public string Name { get; init; }
        
        public string OldFingerprint { get; init; }
        
        public string NewFingerprint { get; init; }
        
        public int Loadouts { get; init; }
        public int LoadoutItemGroups { get; init; }
        public int Files { get; init; }
        public int Collections { get; init; }
        public string Created { get; init; }
    }
    

    public static IEnumerable<object[]> DatabaseNames()
    {
        var databaseFolder = DatabaseFolder();
        foreach (var file in databaseFolder.EnumerateFiles("*.zip").Order())
        {
            yield return [file.Name];
        }
    }

    private static AbsolutePath DatabaseFolder()
    {
        var basePath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Parent.Parent.Parent;
        var databaseFolder = basePath.Combine("Resources/Databases");
        return databaseFolder;
    }
}
