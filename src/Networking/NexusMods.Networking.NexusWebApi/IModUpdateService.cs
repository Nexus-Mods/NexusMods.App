using DynamicData.Kernel;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;
namespace NexusMods.Networking.NexusWebApi;

/// <summary>
///     Provides services related to updating of mods.
/// </summary>
public interface IModUpdateService
{
    /// <summary>
    /// Checks for available updates and updates mod information from Nexus.
    /// After a successful update check, every file is notified of a potential update.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <returns>Updated mod information</returns>
    Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateMods(CancellationToken token);

    /// <summary>
    /// Returns an observable for the newest version of a file.
    /// </summary>
    /// <returns>An observable that signals the newest version of a file.</returns>
    public IObservable<NexusModsFileMetadata.ReadOnly> GetNewestVersionObservable(NexusModsFileMetadata.ReadOnly current);
}
