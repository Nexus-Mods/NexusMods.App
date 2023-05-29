using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Mods;

namespace NexusMods.DataModel.ModInstallers;

/// <summary>
/// Return value of <see cref="IModInstaller"/>. Maps to <see cref="Mod"/> in <see cref="LoadoutManager"/>.
/// </summary>
public record ModInstallerResult
{
    /// <summary>
    /// Unique identifier of the mod.
    /// </summary>
    public required ModId Id { get; init; }

    /// <summary>
    /// All files belonging to the mod.
    /// </summary>
    public required IEnumerable<AModFile> Files { get; init; }

    /// <summary>
    /// Optional name of the mod.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional version of the mod.
    /// </summary>
    public string? Version { get; set; }
}
