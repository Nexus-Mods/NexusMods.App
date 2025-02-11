using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests;

public class LegacyDatabaseSupportTests(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Theory]
    [MemberData(nameof(DatabaseNames))]
    public async Task TestDatabase(string name)
    {
        await using var tempConnection = await ConnectionFor(name);
        
        await Verify(GetStatistics(tempConnection.Connection.Db, name, tempConnection.OldId)).UseParameters(name);
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
}
