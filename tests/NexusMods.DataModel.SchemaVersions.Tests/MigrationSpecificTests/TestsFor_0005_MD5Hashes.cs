using FluentAssertions;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Library.Models;
using Xunit.Abstractions;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0005_MD5Hashes(ITestOutputHelper helper) : ALegacyDatabaseTest(helper)
{
    [Fact]
    public async Task NoMd5()
    {
        await using var tempConnection = await ConnectionFor("Migration-5.rocksdb.zip");

        var db = tempConnection.Connection.Db;

        var localFilesWithMd5 = LocalFile.All(db).Select(static entity => LibraryFile.Md5.IsIn(entity)).ToArray();
        var directWithMd5 = DirectDownloadLibraryFile.All(db).Select(static entity => LibraryFile.Md5.IsIn(entity)).ToArray();

        localFilesWithMd5.Should().AllSatisfy(b => b.Should().BeTrue());
        directWithMd5.Should().AllSatisfy(b => b.Should().BeTrue());
    }
}
