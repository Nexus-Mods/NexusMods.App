namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Specifies the 'type' of loadout that the current loadout represents.
/// </summary>
public enum LoadoutKind : byte
{
    /// <summary>
    /// This is a regular loadout that's created by the user
    /// </summary>
    Default,
    
    /// <summary>
    /// This is loadout that is created from the original game state.
    /// It has a lifetime equal to that of how long we manage an individual
    /// game installation.
    ///
    /// Only when a game is unmanaged do we remove the vanilla state loadout.
    /// This loadout should not be modified, unless we are ingesting game updates.
    /// </summary>
    VanillaState,
    
    /// <summary>
    /// This loadout has been deleted and is marked for garbage collection.
    /// </summary>
    /// <remarks>
    ///     This is expressed as a separate loadout kind because retractions
    ///     in MnemonicDB function as records. Storing this as a property
    ///     allows us to therefore more efficiently delete a loadout.
    /// </remarks>
    Deleted,
}
