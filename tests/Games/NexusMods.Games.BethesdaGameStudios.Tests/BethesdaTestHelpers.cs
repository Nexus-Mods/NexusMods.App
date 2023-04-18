using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

public static class BethesdaTestHelpers
{
    /// <summary>
    /// Returns location of the 'Resources' folder.
    /// </summary>
    public static AbsolutePath GetDownloadableModFolder(IFileSystem fs, string folderName) =>
        GetAssetsPath(fs).CombineUnchecked("DownloadableMods").CombineUnchecked(folderName);

    /// <summary>
    /// Returns location of the 'Assets' folder.
    /// </summary>
    /// <param name="fs">Filesystem to read assets from.</param>
    public static AbsolutePath GetAssetsPath(IFileSystem fs) => fs.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("Assets");

    /// <summary>
    /// Returns location of an individual asset.
    /// </summary>
    /// <param name="fs">Filesystem to read assets from.</param>
    /// <param name="assetRelativePath">Relative path of the asset.</param>
    public static AbsolutePath GetAsset(IFileSystem fs, RelativePath assetRelativePath) => GetAssetsPath(fs).CombineUnchecked(assetRelativePath);
}
