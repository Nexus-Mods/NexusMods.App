namespace NexusMods.Abstractions.Loadouts.Mods;

[Obsolete(message: "This will be removed with Loadout Items")]
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
