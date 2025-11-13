using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Loadouts;
using Xunit.Abstractions;

namespace NexusMods.DataModel.Tests.Undo;

public class RestorePointTests(ITestOutputHelper helper) : AArchivedDatabaseTest(helper)
{
    [Fact]
    public async Task CanGetRestorePoints()
    {
        // Load up a database with two collections installed, and the first one deleted
        await using var tmpConn = await ConnectionFor("two_sdv_collections_added_removed.zip");

        var undoService = tmpConn.Host.Services.GetRequiredService<UndoService>();

        var loadout = Loadout.All(tmpConn.Connection.Db).First();

        var restorePoints = undoService.RevisionsFor(loadout)
            .OrderBy(row => row.Revision.Timestamp);

        // Get the restore points before hand
        var beforePoints = restorePoints.Select(row => new
            {
                TxId = row.Revision.TxEntity.ToString(),
                ModCount = row.ModCount
            }
        ).ToArray();
        
        // Revert to a point where both collections were installed
        var toRevertTo = restorePoints.Skip(2).First().Revision;
        await undoService.RevertTo(toRevertTo);
        
        // Get the restore points after restoring the collection
        var afterPoints = restorePoints.Select(row => new
            {
                TxId = row.Revision.TxEntity.ToString(),
                ModCount = row.ModCount
            }
        );

        // Verify the mod counts and restore points
        await Verify(new { Before = beforePoints, After = afterPoints });
    }

    /// <summary>
    /// During testing, we found that undoing certain transactions could corrupt the database (due to a sorting bug)
    /// </summary>
    [Fact]
    public async Task UndoDoesntCorruptTheDatabase()
    {
        // NOTE(erri120): The database being loaded is broken as it contains a duplicated collection that fail unique assertions

        // Load up a database with two collections installed, and the first one deleted
        await using var tmpConn = await ConnectionFor("corruption_error_db.zip");

        var undoService = tmpConn.Host.Services.GetRequiredService<UndoService>();

        var loadout = Loadout.All(tmpConn.Connection.Db).First(l => l.Id == EntityId.From(0x200000000003755));

        var restorePoints = undoService.RevisionsFor(loadout)
            .OrderBy(row => row.Revision.Timestamp);

        // This would cause a crash before the fix
        await tmpConn.Connection.FlushAndCompact();
        for (var i = 0; i < 2; i++)
        {
            var points = restorePoints.ToArray();
            var pnt = points[i % 2 == 0 ? 1 : 6];
            await undoService.RevertTo(pnt.Revision);
        }
        
        var afterPoints = restorePoints.Select(row => new
            {
                TxId = row.Revision.TxEntity.ToString(),
                ModCount = row.ModCount
            }
        );
    }

    
}
