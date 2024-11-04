using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Storage;
using NexusMods.MnemonicDB.Storage.RocksDbBackend;
using NexusMods.Paths;

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
        
        await Verify(GetStatistics(connection.Db, name)).UseParameters(name);
    }

    private Statistics GetStatistics(IDb db, string name)
    {
        var timestampAttr = MnemonicDB.Abstractions.BuiltInEntities.Transaction.Timestamp;
        return new Statistics
        {
            Name = name,
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
