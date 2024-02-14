using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Extensions related to the <see cref="GamePath"/> class that cannot be stored inside
/// the Paths library due to requiring additional types.
/// </summary>
public static class GamePathExtensions
{
    /// <summary>
    /// Joins the current absolute path with a relative path.
    /// </summary>
    /// <param name="path">Path to the individual game path.</param>
    /// <param name="installation">
    ///    The game installation to combine with the <see cref="Path"/>.
    /// </param>
    public static AbsolutePath CombineChecked(this GamePath path, GameInstallation installation)
    {
        return path.Combine(installation.LocationsRegister[path.LocationId]);
    }
}
