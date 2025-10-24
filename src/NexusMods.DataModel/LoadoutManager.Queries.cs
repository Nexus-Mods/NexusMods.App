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
        if (results.Length == 0) return ConflictPriority.From(1);

        var max = results[0];
        return ConflictPriority.From(max + 1);
    }
}
