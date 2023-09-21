using NexusMods.Paths;

namespace NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;

/// <summary>
/// Represents a target used for where user mods may be manually installed when using human assisted installers
/// such as the Advanced Installer.
/// </summary>
public interface IModInstallDestination
{
    /// <summary>
    /// GamePath to which the relative mod file paths should appended to.
    /// </summary>
    public GamePath DestinationGamePath { get; }

    /// <summary>
    /// List of known recognizable file extensions for direct children of the target <see cref="DestinationGamePath"/>.
    /// NOTE: Only include file extensions that are only likely to appear at this level of the folder hierarchy.
    /// </summary>
    public IEnumerable<Extension> KnownValidFileExtensions { get; }

    /// <summary>
    /// List of file extensions to discard when installing to this target.
    /// </summary>
    public IEnumerable<Extension> FileExtensionsToDiscard { get; }
}

/// <summary>
/// Helper methods for <see cref="IModInstallDestination"/>.
/// </summary>
public static class ModInstallDestinationHelpers
{
    /// <summary>
    /// Converts a list of <see cref="InstallFolderTarget"/>(s) into <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="target">The target to create a mod install destination from.</param>
    /// <param name="accumulator">List to return the results into.</param>
    public static void AddInstallFolderTarget(InstallFolderTarget target, List<IModInstallDestination> accumulator)
    {
        accumulator.Add(target);
        foreach (var subTarget in target.SubTargets)
            AddInstallFolderTarget(subTarget, accumulator);
    }

    /// <summary>
    /// Converts a list of <see cref="InstallFolderTarget"/>(s) into <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="targets">Collection of targets to get destinations from.</param>
    /// <param name="accumulator">List to return the results into.</param>
    public static void AddInstallFolderTargets(IEnumerable<InstallFolderTarget> targets, List<IModInstallDestination> accumulator)
    {
        foreach (var target in targets)
            AddInstallFolderTarget(target, accumulator);
    }

    /// <summary>
    /// Converts a list of <see cref="InstallFolderTarget"/>(s) into <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="locations">Locations to add to the accumulator.</param>
    /// <param name="accumulator">List to return the results into.</param>
    public static void AddCommonLocations(IReadOnlyDictionary<GameFolderType, AbsolutePath> locations, List<IModInstallDestination> accumulator)
    {
        foreach (var location in locations)
        {
            accumulator.Add(new InstallFolderTarget()
            {
                DestinationGamePath = new GamePath(location.Key, location.Value),
                KnownSourceFolderNames = Array.Empty<string>(),
                KnownValidSubfolders = Array.Empty<string>(),
                KnownValidFileExtensions = Array.Empty<Extension>(),
                FileExtensionsToDiscard = Array.Empty<Extension>(),
                SubPathsToDiscard = Array.Empty<RelativePath>()
            });
        }
    }

    /// <summary>
    /// Converts a list of <see cref="InstallFolderTarget"/>(s) into <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="targets">Collection of targets to get destinations from.</param>
    /// <returns>Collection of destinations.</returns>
    public static List<IModInstallDestination> FromInstallFolderTargets(IEnumerable<InstallFolderTarget> targets)
    {
        var result = new List<IModInstallDestination>();
        AddInstallFolderTargets(targets, result);
        return result;
    }
}
