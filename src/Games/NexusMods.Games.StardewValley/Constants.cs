using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.StardewValley;

public class Constants
{
    public static readonly RelativePath ModsFolder = "Mods".ToRelativePath();
    public static readonly RelativePath ManifestFile = "manifest.json".ToRelativePath();
}
