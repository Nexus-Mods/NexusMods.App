namespace NexusMods.Abstractions.Loadouts.Mods;

public enum ModCategory
{
    GameFiles,
    Mod,
    Saves,
    Overrides,
    /// <summary>
    /// Files maintained by the game's framework, not intended for direct user modification.
    /// </summary>
    Metadata,
}
