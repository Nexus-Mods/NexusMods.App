using NexusMods.Abstractions.Diagnostics;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Loadouts.Synchronizers.Rules;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes.Emitters;

public class NoWayToSourceFilesOnDisk : ILoadoutDiagnosticEmitter
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
            if (node.Loadout.Hash == node.Disk.Hash && node.SourceItemType == LoadoutSourceItemType.Game && !node.Signature.HasFlag(Signature.DiskArchived))
            {
                totalSize += node.Loadout.Size;
                count++;
            }
        }

        if (count > 0)
        {
            yield return Diagnostics.CreateGameFilesDoNotHaveSource(totalSize, count);
        }
    }
}
