using FluentAssertions;
using NexusMods.Abstractions.Loadouts;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0007_AddSortOrderParentEntity(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    
    [Fact]
    public async Task Test()
    {
        await using var tempConnection = await ConnectionFor("Migration-7.rocksdb.zip");

        var db = tempConnection.Connection.Db;

        // Get all SortOrder entities that do not have the ParentEntity attribute
        var sortOrders = SortOrder.All(db);

        // Assert that all SortOrders have the ParentEntity attribute set to the Loadout's EntityId
        foreach (var sortOrder in sortOrders)
        {
            sortOrder.IsValid().Should().BeTrue("SortOrders should be valid after migration 7");
            var parentEntity = sortOrder.ParentEntity;
            parentEntity.Should().NotBeNull("ParentEntity should be set after migration 7");
            parentEntity.IsT0.Should().BeTrue("ParentEntity should be a LoadoutId after migration 7");
            parentEntity.AsT0.Value.Should().BeEquivalentTo(sortOrder.LoadoutId.Value, "ParentEntity should match the LoadoutId of the SortOrder after migration 7");
        }
    }
}
