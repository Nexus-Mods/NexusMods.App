using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Files.Diff;
using NexusMods.Abstractions.Loadouts.Exceptions;
using NexusMods.Abstractions.Loadouts.Ids;
using R3;

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
    /// <throws cref="ExecutableInUseException">Thrown if the game EXE is in use, meaning that it's running.</throws>
    public Task Synchronize(LoadoutId loadout);
    
    /// <summary>
    /// Get the diff tree of the unapplied changes of a loadout.
    /// </summary>
    public FileDiffTree GetApplyDiffTree(LoadoutId loadout);

    /// <summary>
    /// Computes whether there are changes that need to be synchronized for a given loadout.
    /// </summary>
    public bool GetShouldSynchronize(LoadoutId loadoutId);

    /// <summary>
    /// Returns the last applied loadout for a given game installation.
    /// </summary>
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Returns an observable of the last applied revisions for a specific game installation
    /// </summary>
    public IObservable<LoadoutWithTxId> LastAppliedRevisionFor(GameInstallation gameInstallation);

    /// <summary>
    /// Returns an observable of the status of the synchronizer for a specific game installation.
    /// </summary>
    public IObservable<GameSynchronizerState> StatusForGame(GameInstallMetadataId gameInstallId);
    
    /// <summary>
    /// Returns an observable of the status of the synchronizer for a specific loadout.
    /// </summary>
    public Task<IObservable<LoadoutSynchronizerState>> StatusForLoadout(LoadoutId loadoutId);
}

