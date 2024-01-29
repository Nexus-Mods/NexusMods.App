using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Games.Downloads;
using NexusMods.Abstractions.Games.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// A component that can index and install archives.
/// </summary>
public interface IArchiveInstaller
{
    /// <summary>
    /// The activity group for archive installer activities.
    /// </summary>
    public static readonly ActivityGroup Group = ActivityGroup.From("ArchiveInstaller");

    /// <summary>
    /// Adds the given archive to the loadout (adding the contents as a mod). This is the
    /// standard way to "install" a mod. If the installer is not provided, the installers on the loadout's game
    /// will be tried, in the order they are defined, otherwise the provided installer will be used.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="downloadId"></param>
    /// <param name="defaultModName"></param>
    /// <param name="installer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ModId[]> AddMods(LoadoutId loadoutId, DownloadId downloadId, string? defaultModName = null, IModInstaller? installer = null, CancellationToken token = default);
}
