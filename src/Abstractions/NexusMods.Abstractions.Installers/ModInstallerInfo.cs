using JetBrains.Annotations;
using NexusMods.Abstractions.FileStore.ArchiveMetadata;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts.Ids;
using NexusMods.Abstractions.Loadouts.Mods;
using ModFileTreeNode = NexusMods.Paths.Trees.KeyedBox<NexusMods.Paths.RelativePath, NexusMods.Abstractions.FileStore.Trees.ModFileTree>;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Contains the information passed to a mod installer.
/// </summary>
[PublicAPI]
public record ModInstallerInfo
{
    /// <summary>
    /// The name of the game.
    /// </summary>
    public required string GameName { get; init; }

    /// <summary>
    /// The name of the mod.
    /// </summary>
    public required string? ModName { get; init; }

    /// <summary>
    /// The game Version installed.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// The store from which the game has been sourced.
    /// </summary>
    public required GameStore Store { get; init; }

    /// <summary>
    /// Provides access to various FileSystem locations for the game.
    /// </summary>
    public required IGameLocationsRegister Locations { get; init; }

    /// <summary>
    /// The base mod id.
    /// </summary>
    public required ModId BaseModId { get; init; }

    /// <summary>
    /// Files from the archive.
    /// </summary>
    public required ModFileTreeNode ArchiveFiles { get; init; }

    /// <summary>
    /// Gets the metadata attached to the downloaded archive.
    /// </summary>
    public required DownloadAnalysis.Model Source { get; init; }
}
