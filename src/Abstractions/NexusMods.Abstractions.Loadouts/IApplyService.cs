﻿using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.Serialization.DataModel.Ids;

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
    /// <param name="loadoutId"></param>
    /// <returns></returns>
    public Task<Loadout> Apply(LoadoutId loadoutId);
    
    /// <summary>
    /// Get the diff tree of the unapplied changes of a loadout.
    /// </summary>
    public ValueTask<FileDiffTree> GetApplyDiffTree(LoadoutId loadoutId);

    
    /// <summary>
    /// Ingest any detected outside changes into the last applied loadout.
    /// This will also rebase any unapplied changes on top of the last applied state, but will not apply them to disk.
    /// The loadoutId will point to the new merged loadout
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns>The merged loadout</returns>
    public Task<Loadout> Ingest(GameInstallation gameInstallation);

    /// <summary>
    /// Returns the last applied loadout for a given game installation.
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns>A tuple of the LoadoutId and loadout revision Id of the last applied state</returns>
    public IId? GetLastAppliedLoadout(GameInstallation gameInstallation);
    
    /// <summary>
    /// Returns an observable of the last applied revisions for a specific game installation
    /// </summary>
    /// <param name="gameInstallation"></param>
    /// <returns></returns>
    public IObservable<IId> LastAppliedRevisionFor(GameInstallation gameInstallation);    
}
