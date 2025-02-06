using FluentAssertions;
using NexusMods.Abstractions.NexusModsLibrary;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0003_FixDuplicates(IServiceProvider serviceProvider) : ALegacyDatabaseTest(serviceProvider)
{
    [Fact]
    public async Task No_Duplicates()
    {
        using var tempConnection = await ConnectionFor("Issue-2608.rocksdb.zip");

        var db = tempConnection.Connection.Db;

        var duplicateFiles = NexusModsFileMetadata
            .All(db)
            .GroupBy(static file => file.Uid)
            .Where(static grouping => grouping.Count() > 1)
            .ToDictionary(static grouping => grouping.Key, static grouping => grouping.ToArray());

        var duplicateMods = NexusModsModPageMetadata
            .All(db)
            .GroupBy(static mod => mod.Uid)
            .Where(static grouping => grouping.Count() > 1)
            .ToDictionary(static grouping => grouping.Key, static grouping => grouping.ToArray());

        duplicateFiles.Should().BeEmpty();
        duplicateMods.Should().BeEmpty();
    }
}
