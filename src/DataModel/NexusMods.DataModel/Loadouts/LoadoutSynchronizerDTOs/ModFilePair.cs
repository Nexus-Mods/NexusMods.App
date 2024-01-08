using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;

/// <summary>
/// A pairing of a mod file and the mod it belongs to.
/// </summary>
public record ModFilePair
{
    /// <summary>
    /// The mod
    /// </summary>
    public required Mod Mod {get; init;}
    
    /// <summary>
    /// The file in the mod
    /// </summary>
    public required AModFile File { get; init; }
}
