using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.ModInstallers;

public interface IModInstallerEx
{
    /// <summary>
    /// Finds all mods inside the provided archive.
    /// </summary>
    /// <param name="gameInstallation">The game installation.</param>
    /// <param name="baseModId">The base mod id.</param>
    /// <param name="srcArchiveHash">Hash of the source archive.</param>
    /// <param name="archiveFiles">Files from the archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsyncEx(
        GameInstallation gameInstallation,
        ModId baseModId,
        Hash srcArchiveHash,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        CancellationToken cancellationToken = default);
}
