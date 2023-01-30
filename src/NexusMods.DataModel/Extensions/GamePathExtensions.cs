using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.DataModel.Extensions;

public static class GamePathExtensions
{
    public static AbsolutePath RelativeTo(this GamePath path, GameInstallation installation)
    {
        return path.RelativeTo(installation.Locations[path.Folder]);
    }
}