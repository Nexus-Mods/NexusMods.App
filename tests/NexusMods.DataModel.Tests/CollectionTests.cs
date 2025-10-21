using FluentAssertions;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests;

public class CollectionTests(ITestOutputHelper helper) : AArchivedDatabaseTest(helper)
{
    [Fact]
    public async Task CanMakeNexusModsCollectionsEditable()
    {
        // Load up a database with two collections installed, and the first one deleted
        await using var tmpConn = await ConnectionFor("two_sdv_collections_added_removed.zip");
        var conn = tmpConn.Connection;
        
        var collId = tmpConn.Connection.Query<EntityId>($"SELECT Id FROM mdb_NexusCollectionLoadoutGroup(Db => {tmpConn.Connection.Db}) ORDER BY Name DESC").First();
        var coll = NexusCollectionLoadoutGroup.Load(tmpConn.Connection.Db, collId);
        
        coll.AsCollectionGroup().AsLoadoutItemGroup().AsLoadoutItem().Name.Should().BeEquivalentTo("Aesthetic Valley | Witchcore");

        var newId = await NexusCollectionLoadoutGroup.MakeEditableLocalCollection(tmpConn.Connection, coll, "[Copy Of] Aesthetic Valley | Witchcore");
        var newColl = CollectionGroup.Load(tmpConn.Connection.Db, newId);
        
        newColl.AsLoadoutItemGroup().AsLoadoutItem().Name.Should().BeEquivalentTo("[Copy Of] Aesthetic Valley | Witchcore");
        newColl.IsReadOnly.Should().BeFalse();

        NexusCollectionLoadoutGroup.Collection.Contains(newColl).Should().BeFalse();
        NexusCollectionLoadoutGroup.Collection.Contains(coll).Should().BeTrue();
        
        NexusCollectionLoadoutGroup.Revision.Contains(newColl).Should().BeFalse();
        NexusCollectionLoadoutGroup.Revision.Contains(coll).Should().BeTrue();
        
        NexusCollectionLoadoutGroup.LibraryFile.Contains(newColl).Should().BeFalse();
        NexusCollectionLoadoutGroup.LibraryFile.Contains(coll).Should().BeTrue();

        conn.Query<EntityId>($"SELECT Id FROM mdb_NexusCollectionItemLoadoutGroup(Db => {conn}) WHERE Parent = {newColl.Id}")
            .Should()
            .HaveCount(0);

        conn.Query<EntityId>($"SELECT Id FROM mdb_LoadoutItemGroup(Db => {conn}) WHERE Parent = {newColl.Id}")
            .Should()
            .HaveCountGreaterThan(0);
    }
}
