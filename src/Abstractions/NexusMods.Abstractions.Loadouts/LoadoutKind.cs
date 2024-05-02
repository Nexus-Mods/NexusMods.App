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
    /// This is a temporary loadout that is created from the original game state
    /// upon removal of the currently active loadout. This loadout is removed
    /// when a 'real' loadout is applied.
    ///
    /// A GameInstallation should only have 1 marker loadout.
    /// </summary>
    Marker,
}
