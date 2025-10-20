using System.Diagnostics;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers.Conflicts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.DataModel;

internal partial class LoadoutManager
{
    private static ConflictPriority GetNextPriority(LoadoutId loadoutId, IDb db)
    {
        var query = db.Connection.Query<ulong>($"SELECT MaxPriority FROM synchronizer.MaxPriority({db}) WHERE Loadout = {loadoutId}");

        // TODO: https://github.com/Nexus-Mods/NexusMods.MnemonicDB/issues/181
        var results = query.ToArray();
        Debug.Assert(results.Length == 1, $"scalar query should return 1 element, found {results.Length} elements instead");
        var max = results[0];
        return ConflictPriority.From(max + 1);
    }
}
