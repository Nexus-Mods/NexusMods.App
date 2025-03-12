using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.Emitters;

public class UndeployableLoadoutDueToMissingGameFiles : ILoadoutDiagnosticEmitter
{
    public async IAsyncEnumerable<Diagnostic> Diagnose(Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {

        var syncronizer = loadout.InstallationInstance.GetGame().Synchronizer;
        var syncTree = await syncronizer.BuildSyncTree(loadout);
        syncronizer.ProcessSyncTree(syncTree);
        
        var totalSize = Size.Zero;
        var count = 0;
        
        foreach (var (_, node) in syncTree)
        {
            if (node.SourceItemType == LoadoutSourceItemType.Game && node.Actions.HasFlag(Actions.WarnOfUnableToExtract))
            {
                totalSize += node.Loadout.Size;
                count++;
            }
        }

        if (count > 0)
        {
            yield return Diagnostics.CreateUndeployableLoadoutDueToMissingGameFiles(
                Size: totalSize,
                FileCount: count,
                Game: loadout.InstallationInstance.Game.Name,
                Store: loadout.InstallationInstance.Store.Value,
                Version: loadout.GameVersion.ToString()
            );
        }
    }
}
