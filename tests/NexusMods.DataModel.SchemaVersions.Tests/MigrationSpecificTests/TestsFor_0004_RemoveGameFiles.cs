using FluentAssertions;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

/// <summary>
/// Test the migration to remove game files and add LocatorIds and GameVersions to Loadouts.
/// </summary>
/// <param name="provider"></param>
public class TestsFor_0004_RemoveGameFiles(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Theory]
    [MethodData(nameof(DatabaseNames))]
    public async Task Test(string databaseName)
    {
        await using var tempConnection = await ConnectionFor(databaseName);
        var db = tempConnection.Connection.Db;
        foreach (var loadout in Loadout.All(db))
        {
            loadout.Contains(Loadout.LocatorIds).Should().BeTrue("Loadout should contain LocatorIds");
            loadout.Contains(Loadout.GameVersion).Should().BeTrue("Loadout should contain GameVersion");
        }

        var gameGroupAttrs = db.AttributeCache.AllAttributeIds
            .Where(sym => sym.Namespace == "NexusMods.Loadouts.LoadoutGameFilesGroup")
            .Select(sym => (sym, db.AttributeCache.GetAttributeId(sym)))
            .ToArray();
        
        foreach (var (sym, attrId) in gameGroupAttrs)
        {
            db.Datoms(SliceDescriptor.Create(attrId)).Should().BeEmpty($"All datoms with {sym} should be removed");
        }
    }
    
}
