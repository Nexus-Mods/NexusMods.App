using JetBrains.Annotations;
using NexusMods.Abstractions.DataModel.Entities.Sorting;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Return value of <see cref="IModInstaller"/>.
/// </summary>
[PublicAPI]
public record ModInstallerResult
{
    /// <summary>
    /// Unique identifier of the mod.
    /// </summary>
    public required ModId? Id { get; init; }

    /// <summary>
    /// All files belonging to the mod.
    /// </summary>
    public required IEnumerable<TempEntity> Files { get; init; }
    
    /// <summary>
    /// Optional name of the mod.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Optional version of the mod.
    /// </summary>
    public string? Version { get; init; }
    
    /// <summary>
    /// Extra metadata for the mod.
    /// </summary>
    public TempEntity? Metadata { get; init; }
}
