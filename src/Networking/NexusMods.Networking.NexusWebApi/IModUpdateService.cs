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
    /// After a successful update check, every file and page is notified of a potential update if
    /// the parameter to <paramref name="notify"/> is set to true.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <param name="notify">True if external listeners should be notified after an update check.</param>
    /// <returns>Updated mod information</returns>
    Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateModPages(CancellationToken token, bool notify = true);

    /// <summary>
    /// Notifies of updates to mod files and mod pages based on our existing metadata.
    /// If you need more up-to-date data, call <see cref="CheckAndUpdateModPages"/>.
    /// </summary>
    /// <remarks>
    ///     Used when opening new views in the App, without an explicit update check,
    ///     e.g. switching or opening new views.
    /// </remarks>
    void NotifyForUpdates();
    
    /// <summary>
    /// Returns an observable for the newest version of a file.
    /// </summary>
    /// <returns>An observable that signals the newest version of a file.</returns>
    IObservable<Optional<NexusModsFileMetadata.ReadOnly>> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current);

    /// <summary>
    /// Returns an observable when any file on a mod page is updated. 
    /// </summary>
    /// <param name="current">The current mod page to listen for changes in.</param>
    IObservable<Optional<NewerFilesOnModPage>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current);
}
