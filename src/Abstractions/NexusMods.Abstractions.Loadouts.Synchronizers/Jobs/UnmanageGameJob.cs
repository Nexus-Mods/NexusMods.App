using NexusMods.Abstractions.DurableJobs;
using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A job that unmanages a game
/// </summary>
public class UnmanageGameJob : AUnitOfWork<UnmanageGameJob, GameInstallation, GameInstallation, bool>
{
    protected override async Task<GameInstallation> Start(GameInstallation install, bool runGC, CancellationToken token)
    {
        var synchronizer = (ALoadoutSynchronizer)((IGameWithSynchronizer)install.Game).Synchronizer;
        
        await synchronizer.UnManageInternal(install, runGC, token);
        return install;
    }
}
