using JetBrains.Annotations;
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
[PublicAPI]
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
    /// Finds all mods inside the provided archive.
    /// </summary>
    /// <param name="gameInstallation">The game installation.</param>
    /// <param name="baseMod">The base mod.</param>
    /// <param name="srcArchiveHash">Hash of the source archive.</param>
    /// <param name="archiveFiles">Files from the archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public ValueTask<IEnumerable<Mod>> GetModsAsync(
        GameInstallation gameInstallation,
        Mod baseMod,
        Hash srcArchiveHash,
        EntityDictionary<RelativePath, AnalyzedFile> archiveFiles,
        CancellationToken cancellationToken = default);
}
