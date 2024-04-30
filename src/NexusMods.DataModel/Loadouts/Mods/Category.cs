using Vogen;

namespace NexusMods.DataModel.Loadouts.Mods;

/// <summary>
/// Represents a category of a mod.
/// </summary>
[ValueObject<string>]
public readonly partial struct Category
{
    /// <summary>
    /// Files that are part of the game itself, the base game files.
    /// </summary>
    public static Category GameFiles = From("GameFiles");
}
