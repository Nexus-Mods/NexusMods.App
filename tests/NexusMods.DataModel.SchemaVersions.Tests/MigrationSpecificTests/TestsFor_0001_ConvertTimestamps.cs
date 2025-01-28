
using FluentAssertions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;

namespace NexusMods.DataModel.SchemaVersions.Tests.MigrationSpecificTests;

public class TestsFor_0001_ConvertTimestamps(IServiceProvider provider) : ALegacyDatabaseTest(provider)
{
    [Fact]
    public async Task OldTimestampsAreInRange()
    {
        using var tempConnection = await ConnectionFor("SDV.4_11_2024.rocksdb.zip");

        var txTimes = tempConnection.Connection.Db.Datoms(Transaction.Timestamp)
            .Resolved(tempConnection.Connection)
            .OfType<TimestampAttribute.ReadDatom>()
            .Select(d => d.V.Year)
            .ToArray();
        
        txTimes.Should().NotBeEmpty();

        foreach (var year in txTimes)
        {
            year.Should().BeGreaterOrEqualTo(2024).And.BeLessOrEqualTo(DateTimeOffset.UtcNow.Year);
        }
    }
}
