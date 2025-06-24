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
    /// <param name="throttle">Whether to throttle the update check to specific intervals.</param>
    /// <returns>
    ///     Updated mod page information.
    ///     Calls to this method may be throttled if <paramref name="throttle"/> is set to false.
    /// </returns>
    Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateModPages(CancellationToken token, bool notify = true, bool throttle = true);

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
    /// <param name="current">The current file to listen for changes in.</param>
    /// <param name="select">
    ///     A selector that can be used to transform or discard notifications about file updates. Return null to discard.
    ///     If null is passed, default filters will be applied automatically.
    ///     To get unfiltered data, pass an empty filter function that returns the input unchanged.
    ///     (or use <see cref="ModUpdateServiceExtensions.GetNewestFileVersionObservableUnfiltered"/>).
    /// </param>
    /// <returns>An observable that signals an update for a singular mod on a page.</returns>
    IObservable<Optional<ModUpdateOnPage>> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current, Func<ModUpdateOnPage, ModUpdateOnPage?>? select = null);

    /// <summary>
    /// Returns an observable when any file on a mod page is updated.
    /// </summary>
    /// <param name="current">The current mod page to listen for changes in.</param>
    /// <param name="select">
    ///     A selector that can be used to transform or discard notifications about mod page updates. Return null to discard.
    ///     If null is passed, default filters will be applied automatically.
    ///     To get unfiltered data, pass an empty filter function that returns the input unchanged.
    ///     (or use <see cref="ModUpdateServiceExtensions.GetNewestModPageVersionObservableUnfiltered"/>).
    /// </param>
    /// <returns>An observable that returns all updated items on a given mod page.</returns>
    IObservable<Optional<ModUpdatesOnModPage>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current, Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null);

    /// <summary>
    /// Checks if a mod page has updates available without creating an observable.
    /// This is a non-observable version of <see cref="GetNewestModPageVersionObservable"/> that checks the current cache state.
    /// </summary>
    /// <param name="current">The current mod page to check for updates.</param>
    /// <param name="select">
    ///     A selector that can be used to transform or discard update results. Return null to discard.
    ///     If null is passed, default filters will be applied automatically.
    /// </param>
    /// <returns>The current update state for the mod page, if any updates are available.</returns>
    Optional<ModUpdatesOnModPage> HasModPageUpdatesAvailable(NexusModsModPageMetadata.ReadOnly current, Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null);
}


/// <summary>
/// Extension methods for <see cref="IModUpdateService"/> providing unfiltered access to update data.
/// </summary>
public static class ModUpdateServiceExtensions
{
    /// <summary>
    /// Returns an observable for the newest version of a file without applying any default filters.
    /// Use this when you need access to raw update data.
    /// </summary>
    /// <param name="service">The mod update service.</param>
    /// <param name="current">The current file to listen for changes in.</param>
    /// <param name="select">
    ///     A selector that can be used to transform or discard notifications about file updates. Return null to discard.
    ///     Pass an empty function (x => x) if you want truly unfiltered data.
    /// </param>
    /// <returns>An observable that signals an update for a singular mod on a page.</returns>
    public static IObservable<Optional<ModUpdateOnPage>> GetNewestFileVersionObservableUnfiltered(
        this IModUpdateService service, 
        NexusModsFileMetadata.ReadOnly current, 
        Func<ModUpdateOnPage, ModUpdateOnPage?>? select = null)
    {
        // Pass an identity function to bypass default filtering
        return service.GetNewestFileVersionObservable(current, select ?? (x => x));
    }

    /// <summary>
    /// Returns an observable when any file on a mod page is updated without applying any default filters.
    /// Use this when you need access to raw update data.
    /// </summary>
    /// <param name="service">The mod update service.</param>
    /// <param name="current">The current mod page to listen for changes in.</param>
    /// <param name="select">
    ///     A selector that can be used to transform or discard notifications about mod page updates. Return null to discard.
    ///     Pass an empty function (x => x) if you want truly unfiltered data.
    /// </param>
    /// <returns>An observable that returns all updated items on a given mod page.</returns>
    public static IObservable<Optional<ModUpdatesOnModPage>> GetNewestModPageVersionObservableUnfiltered(
        this IModUpdateService service,
        NexusModsModPageMetadata.ReadOnly current, 
        Func<ModUpdatesOnModPage, ModUpdatesOnModPage?>? select = null)
    {
        // Pass an identity function to bypass default filtering
        return service.GetNewestModPageVersionObservable(current, select ?? (x => x));
    }
}
