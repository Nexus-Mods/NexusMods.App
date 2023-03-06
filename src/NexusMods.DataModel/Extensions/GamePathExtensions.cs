using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.DataModel.Extensions;

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
        return path.CombineChecked(installation.Locations[path.Type]);
    }
}
