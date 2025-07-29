using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Hashing.xxHash3;
using NexusMods.Networking.NexusWebApi;
using NSubstitute;
using StrawberryShake;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0008_AddCollectionId(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Fact]
    public async Task Test()
    {
        await using var tempConnection = await ConnectionFor("Migration-8.rocksdb.zip");

        var db = tempConnection.Connection.Db;

        var entities = CollectionMetadata.All(db);
        foreach (var entity in entities)
        {
            CollectionMetadata.CollectionId.Contains(entity).Should().BeTrue();
            var expected = entity.Slug.Value.xxHash3AsUtf8().Value;
            entity.CollectionId.Value.Should().Be(expected);
        }
    }
}
