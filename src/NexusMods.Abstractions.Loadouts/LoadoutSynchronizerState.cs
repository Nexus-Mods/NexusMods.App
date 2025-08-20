using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// The synchronization state of a loadout.
/// </summary>
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

public record struct LoadoutSynchronizerStateWithProgress(LoadoutSynchronizerState State, Percent Progress)
{
    public LoadoutSynchronizerStateWithProgress(LoadoutSynchronizerState state) : this(state, Percent.Zero)
    {
    }
}
