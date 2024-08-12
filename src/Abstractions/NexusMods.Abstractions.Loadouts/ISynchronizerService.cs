using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Singleton service for applying loadouts and tracking what loadouts are currently applied/being applied.
/// </summary>
public interface ISynchronizerService
{
    /// <summary>
    /// Synchronize the loadout with the game folder, any changes in the game folder will be added to the loadout, and any
    /// new changes in the loadout will be applied to the game folder.
    /// </summary>
    public Task Synchronize(LoadoutId loadout);
    
    /// <summary>
    /// Get the diff tree of the unapplied changes of a loadout.
    /// </summary>
    public FileDiffTree GetApplyDiffTree(LoadoutId loadout);

    /// <summary>
    /// Returns the last applied loadout for a given game installation.
    /// </summary>
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Returns an observable of the last applied revisions for a specific game installation
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation);


    /// <summary>
    /// Returns an observable of the status of the synchronizer for a specific loadout.
    /// </summary>
    public Task<IObservable<LoadoutSynchronizerState>> StatusFor(LoadoutId loadoutId);
}

