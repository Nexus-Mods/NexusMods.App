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
        await using var tmpConn = await ConnectionFor("with_bloom.zip");

        var undoService = tmpConn.Host.Services.GetRequiredService<UndoService>();

        var loadout = Loadout.All(tmpConn.Connection.Db).First();

        var restorePoints = (await undoService.RevisionsFor(loadout))
            .OrderBy(row => row.Revision.Timestamp);

        var beforePoints = restorePoints.Select(row => new
            {
                TxId = row.Revision.TxEntity.ToString(),
                Timestamp = row.Revision.Timestamp.ToString(),
                ModCount = row.ModCount
            }
        );
        
        await undoService.RevertTo(restorePoints.Skip(2).First().Revision);
        
        restorePoints = (await undoService.RevisionsFor(loadout))
            .OrderBy(row => row.Revision.Timestamp);
        
        var afterPoints = restorePoints.Select(row => new
            {
                TxId = row.Revision.TxEntity.ToString(),
                Timestamp = row.Revision.Timestamp.ToString(),
                ModCount = row.ModCount
            }
        );

        await Verify(new { Before = beforePoints, After = afterPoints });
    }

    
}
