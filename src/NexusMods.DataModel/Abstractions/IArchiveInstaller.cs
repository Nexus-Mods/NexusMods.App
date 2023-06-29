using NexusMods.DataModel.Loadouts;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// A component that can index and install archives.
/// </summary>
public interface IArchiveInstaller
{
    /// <summary>
    /// Adds the given archive to the loadout (adding the contents as a mod). This is the
    /// standard way to "install" a mod.
    /// </summary>
    /// <param name="loadoutId"></param>
    /// <param name="archiveHash"></param>
    /// <param name="defaultModName"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<ModId[]> AddMods(LoadoutId loadoutId, Hash archiveHash, string? defaultModName = null, CancellationToken token = default);
}
