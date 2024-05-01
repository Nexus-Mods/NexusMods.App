using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// A registry for managing disk states created by ingesting/applying loadouts
/// </summary>
public interface IDiskStateRegistry
{
    /// <summary>
    /// Saves a disk state to the data store for the given game installation
    /// </summary>
    /// <param name="installation">The game installation associated with the disk state</param>
    /// <param name="diskState">The disk state to save</param>
    Task SaveState(GameInstallation installation, DiskStateTree diskState);

    /// <summary>
    /// Gets the disk state associated with a specific game installation, returns false if no state is found
    /// </summary>
    /// <param name="gameInstallation">The game installation to retrieve the disk state for</param>
    /// <returns>The disk state associated with the game installation, or null if not found</returns>
    DiskStateTree? GetState(GameInstallation gameInstallation);
    
    /// <summary>
    /// Gets the Loadout Revision Id of the last applied state for a given game installation
    /// </summary>
    public bool TryGetLastAppliedLoadout(GameInstallation gameInstallation, out EntityId loadoutId, out TxId txId);
    
    /// <summary>
    /// Observable of all the last applied revisions for all game installations
    /// </summary>
    IObservable<(GameInstallation gameInstallation, EntityId, TxId)> LastAppliedRevisionObservable { get; }

    /// <summary>
    /// Saves the initial disk state for a given game installation.
    /// </summary>
    /// <param name="installation">The game installation associated with the initial disk state</param>
    /// <param name="diskState">The initial disk state to save</param>
    Task SaveInitialState(GameInstallation installation, DiskStateTree diskState);

    /// <summary>
    /// Retrieves the initial disk state for a given game installation.
    /// </summary>
    /// <param name="installation">The game installation to retrieve the initial disk state for</param>
    /// <returns>The initial disk state, or null if not found</returns>
    DiskStateTree? GetInitialState(GameInstallation installation);
}
