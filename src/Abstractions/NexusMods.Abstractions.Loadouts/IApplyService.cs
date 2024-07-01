using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Singleton service for applying loadouts and tracking what loadouts are currently applied/being applied.
/// </summary>
public interface IApplyService
{
    /// <summary>
    /// Apply a loadout to its game installation.
    /// This will also ingest outside changes and merge unapplied changes on top and apply them.
    /// </summary>
    public Task Apply(Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Synchronize the loadout with the game folder, any changes in the game folder will be added to the loadout, and any
    /// new changes in the loadout will be applied to the game folder.
    /// </summary>
    public Task Synchronize(Loadout.ReadOnly loadout);
    
    /// <summary>
    /// Get the diff tree of the unapplied changes of a loadout.
    /// </summary>
    public FileDiffTree GetApplyDiffTree(Loadout.ReadOnly loadout);

    
    /// <summary>
    /// Ingest any detected outside changes into the last applied loadout.
    /// This will also rebase any unapplied changes on top of the last applied state, but will not apply them to disk.
    /// The loadoutId will point to the new merged loadout
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns>The merged loadout</returns>
    public Task<Loadout.ReadOnly> Ingest(GameInstallation gameInstallation);

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
}
