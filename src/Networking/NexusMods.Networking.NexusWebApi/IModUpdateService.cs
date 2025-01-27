using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;
namespace NexusMods.Networking.NexusWebApi;

/// <summary>
///     Provides services related to updating of mods.
/// </summary>
public interface IModUpdateService
{
    /// <summary>
    /// Checks for available updates and updates mod information from Nexus
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <returns>Updated mod information</returns>
    Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateMods(CancellationToken token);
}
