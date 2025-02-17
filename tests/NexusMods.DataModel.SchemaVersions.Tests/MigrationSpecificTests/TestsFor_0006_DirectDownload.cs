using FluentAssertions;
using NexusMods.Abstractions.Collections;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0006_DirectDownload(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Fact]
    public async Task Test()
    {
        await using var tempConnection = await ConnectionFor("Migration-6.rocksdb.zip");

        var db = tempConnection.Connection.Db;

        var entities = DirectDownloadLibraryFile.All(db);
        foreach (var entity in entities)
        {
            entity.AsLocalFile().IsValid().Should().BeTrue("DirectDownloadLibraryFile should also be a LocalFile after migration 6");
        }
    }
}
