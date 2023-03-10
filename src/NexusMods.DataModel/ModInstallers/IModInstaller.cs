using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModInstallers;

/// <summary>
/// Game specific extension that provides support for the installation of mods
/// (currently archives) to the game folder.
/// </summary>
public interface IModInstaller
{
    /// <summary>
    /// Determines the priority of this installer given the game installation
    /// and contents of an archive. The installer with the highest returned priority
    /// will be used to deploy an archive.
    /// </summary>
    /// <param name="installation">The installation of the game to use.</param>
    /// <param name="files">All of the files within an archive.</param>
    /// <returns>Priority.</returns>
    public Priority Priority(GameInstallation installation, EntityDictionary<RelativePath, AnalyzedFile> files);

    /// <summary>
    /// Determines which files to deploy and pushes them out to FileSystem.
    /// </summary>
    /// <param name="installation">The game installation to push files out to.</param>
    /// <param name="srcArchiveHash">Hash of the source archive.</param>
    /// <param name="files">Files present in the archive.</param>
    /// <returns>A list of files to deploy.</returns>
    public IEnumerable<AModFile> Install(GameInstallation installation, Hash srcArchiveHash, EntityDictionary<RelativePath, AnalyzedFile> files);
}
