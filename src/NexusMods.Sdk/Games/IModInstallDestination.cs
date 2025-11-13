using NexusMods.Paths;

namespace NexusMods.Sdk.Games;

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
    /// Adds a list of common locations (passed via parameter) to accumulator of <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="locations">Locations to add to the accumulator.</param>
    /// <param name="accumulator">List to return the results into.</param>
    public static void AddCommonLocations(IReadOnlyDictionary<LocationId, AbsolutePath> locations, List<IModInstallDestination> accumulator)
    {
        foreach (var location in locations)
        {
            accumulator.Add(new InstallFolderTarget()
            {
                // Locations has
                DestinationGamePath = new GamePath(location.Key, ""),
            });
        }
    }

    /// <summary>
    /// Converts a list of common locations (passed via parameter) to <see cref="IModInstallDestination"/>.
    /// </summary>
    /// <param name="locations">Locations to add to the accumulator.</param>
    public static List<IModInstallDestination> GetCommonLocations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        var result = new List<IModInstallDestination>();
        AddCommonLocations(locations, result);
        return result;
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
