using NexusMods.Abstractions.DurableJobs;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A job that creates a new loadout, possibly indexing and archiving files in the process
/// </summary>
public class CreateLoadoutJob : AUnitOfWork<CreateLoadoutJob, LoadoutId, GameInstallation, string?> 
{
    /// <inheritdoc />
    protected override async Task<LoadoutId> Start(GameInstallation arg1, string? arg2, CancellationToken token)
    {
        var synchronizer = (ALoadoutSynchronizer)((IGameWithSynchronizer)arg1.Game).Synchronizer;
        
        return await synchronizer.CreateLoadoutInternal(arg1, arg2, token);
    }
}
