using NexusMods.Abstractions.DataModel.Entities.Mods;
using NexusMods.Abstractions.Installers.DTO;
using NexusMods.Abstractions.Installers.Info;
using ModFileTreeNode =
    NexusMods.Paths.Trees.KeyedBox<NexusMods.Paths.RelativePath, NexusMods.Abstractions.Installers.Trees.ModFileTree>;

namespace NexusMods.Abstractions.Installers;

/// <summary>
///     Contains the information passed to a mod installer.
/// </summary>
public record ModInstallerInfo
{
    /// <summary>
    ///     The name of the game.
    /// </summary>
    public required string GameName { get; init; }

    /// <summary>
    ///     The name of the mod.
    /// </summary>
    public required string? ModName { get; init; }

    /// <summary>
    ///     The game Version installed.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    ///     The store from which the game has been sourced.
    /// </summary>
    public required GameStore Store { get; init; }

    /// <summary>
    ///     Provides access to various FileSystem locations for the game.
    /// </summary>
    public required IGameLocationsRegister Locations { get; init; }

    /// <summary>
    ///     The base mod id.
    /// </summary>
    public required ModId BaseModId { get; set; }

    /// <summary>
    ///     Files from the archive.
    /// </summary>
    public required ModFileTreeNode ArchiveFiles { get; set; }
}
