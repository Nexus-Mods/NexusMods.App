using Microsoft.Extensions.FileSystemGlobbing;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators.GameCapabilities;

/// <summary>
/// Represents a target path for installing simple mods archives
/// that only need to have their contents placed to a specific game location.
///
/// Each <see cref="InstallFolderTarget"/> represents a single game location and
/// contains information useful for recognizing and installing mod file paths to that location.
/// </summary>
public class InstallFolderTarget : IModInstallDestination
{
    /// <summary>
    /// GamePath to which the relative mod file paths should appended to.
    /// </summary>
    public GamePath DestinationGamePath { get; init; }

    /// <summary>
    /// List of known recognizable aliases that can be directly mapped to the <see cref="DestinationGamePath"/>.
    /// </summary>
    public IEnumerable<RelativePath> KnownSourceFolderNames { get; init; } = [];

    /// <summary>
    /// List of known recognizable first level subfolders of the target <see cref="DestinationGamePath"/>.
    /// NOTE: Only include folders that are only likely to appear at this level of the folder hierarchy.
    /// </summary>
    public IEnumerable<RelativePath> Names { get; init; } = [];

    /// <summary>
    /// List of known recognizable file extensions for direct children of the target <see cref="DestinationGamePath"/>.
    /// NOTE: Only include file extensions that are only likely to appear at this level of the folder hierarchy.
    /// </summary>
    public IEnumerable<Extension> KnownValidFileExtensions { get; init; } = [];

    /// <summary>
    /// List of subPaths of the target <see cref="DestinationGamePath"/> that should be discarded.
    /// </summary>
    public IEnumerable<RelativePath> SubPathsToDiscard { get; init; } = [];

    /// <summary>
    /// List of file extensions to discard when installing to this target.
    /// </summary>
    public IEnumerable<Extension> FileExtensionsToDiscard { get; init; } = [];

    /// <summary>
    /// Collection of Targets that are nested paths relative to <see cref="DestinationGamePath"/>.
    /// </summary>
    public IEnumerable<InstallFolderTarget> SubTargets { get; init; } = [];
}
