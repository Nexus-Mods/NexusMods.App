using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.DataModel.Undo;
using NexusMods.MnemonicDB.Abstractions;
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

        var restorePoints = (await undoService.RevisionsFor(loadout))
            .OrderBy(row => row.TxId);

        // Get the restore points before hand
        var beforePoints = restorePoints.Select(row => new
            {
                TxId = row.TxId.ToString(),
                Added = row.Added,
                Removed = row.Removed,
                Modified = row.Modified,
                MissingGameFiles = row.MissingGameFiles,
            }
        ).ToArray();
        
        // Revert to a point where both collections were installed
        var toRevertTo = restorePoints.Skip(2).First();
        await undoService.RevertTo(toRevertTo.LoadoutId, TxId.From(toRevertTo.TxId.Value));
        
        // Get the restore points after restoring the collection
        var afterPoints = restorePoints.Select(row => new
            {
                TxId = row.TxId.ToString(),
                Added = row.Added,
                Removed = row.Removed,
                Modified = row.Modified,
                MissingGameFiles = row.MissingGameFiles,
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
        // Load up a database with two collections installed, and the first one deleted
        await using var tmpConn = await ConnectionFor("corruption_error_db.zip");

        var undoService = tmpConn.Host.Services.GetRequiredService<UndoService>();

        var loadout = Loadout.All(tmpConn.Connection.Db).First(l => l.Id == EntityId.From(0x200000000003755));

        var restorePoints = (await undoService.RevisionsFor(loadout))
            .OrderBy(row => row.TxId);

        // This would cause a crash before the fix
        await tmpConn.Connection.FlushAndCompact();
        for (var i = 0; i < 2; i++)
        {
            var points = restorePoints.ToArray();
            var pnt = points[i % 2 == 0 ? 1 : 6];
            await undoService.RevertTo(pnt.LoadoutId, TxId.From(pnt.TxId.Value));
        }
        
        var afterPoints = restorePoints.Select(row => new
            {
                TxId = row.TxId.ToString(),
                Added = row.Added,
                Removed = row.Removed,
            }
        );
    }

    
}
