using JetBrains.Annotations;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.DataModel.Loadouts.Markers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Trees;

namespace NexusMods.DataModel.ModInstallers;

/// <summary>
/// Game specific extension that provides support for the installation of mods
/// (currently archives) to the game folder.
/// </summary>
[PublicAPI]
public interface IModInstaller
{
    /// <summary>
    /// Finds all mods inside the provided archive.
    /// </summary>
    /// <param name="gameInstallation">The game installation.</param>
    /// <param name="baseModId">The base mod id.</param>
    /// <param name="loadoutId">Id of the Loadout where mods will be installed.</param>
    /// <param name="archiveFiles">Files from the archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    public ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        GameInstallation gameInstallation,
        LoadoutId loadoutId,
        ModId baseModId,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        CancellationToken cancellationToken = default);
}
