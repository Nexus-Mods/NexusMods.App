namespace NexusMods.Abstractions.Loadouts;

public enum LoadoutSynchronizerState
{
    /// <summary>
    /// The game state is currently being synchronized with the loadout.
    /// </summary>
    Pending,
    
    /// <summary>
    /// Another loadout is currently synced with the game state.
    /// </summary>
    OtherLoadoutSynced,
    
    /// <summary>
    /// The loadout is applied, but needs to be updated to be up-to-date with the game state.
    /// </summary>
    NeedsSync,
    
    /// <summary>
    /// The loadout is applied and up-to-date with the game state.
    /// </summary>
    Current,
}
