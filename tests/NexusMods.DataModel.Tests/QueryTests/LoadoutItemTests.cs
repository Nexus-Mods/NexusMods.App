using System.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.DataModel.Queries;
using NexusMods.DataModel.SchemaVersions.Tests;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests.QueryTests;

public class LoadoutItemTests(ITestOutputHelper helper) : AQueryTest(helper)
{

    [Fact]
    public async Task CanGetEnabledItems()
    {
        for (var i = 0; i < 2; i++)
        {
            var conn = await ConnectionFor("LargeSDVDatabase.rocksdb.zip");

            var sw = Stopwatch.StartNew();
            var enabled = conn.Connection.Topology.Outlet(NexusModsModPageMetadata.Queries.FileStats);
            var e = sw.ElapsedMilliseconds;

            helper.WriteLine($"Elapsed: {e}ms");
            var diagram = conn.Connection.Topology.Diagram();
            await Task.Delay(1);
        }
    }
    
}
