using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.Games.BladeAndSorcery;

public class Constants
{
    public const string ModManifestFileName = "manifest.json";

    private const string AssetsDirectory = "BladeAndSorcery_Data/StreamingAssets";

    public static readonly RelativePath ModsDirectory = $"{AssetsDirectory}/Mods".ToRelativePath();
    public static readonly RelativePath DefaultAssetsDirectory = $"{AssetsDirectory}/Default".ToRelativePath();
}
