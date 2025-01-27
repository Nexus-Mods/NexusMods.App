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
    public IObservable<NexusModsFileMetadata.ReadOnly> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current);

    /// <summary>
    /// Returns an observable when any file on a mod page is updated. 
    /// </summary>
    /// <param name="current">The current mod page to listen for changes in.</param>
    public IObservable<NewestModPageVersionData> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current);
}

/// <summary>
/// Represents the callback
/// </summary>
/// <param name="NewestFile">The newest file within the mod page. We inherit some properties from here.</param>
/// <param name="NumToUpdate">The number of items to be updated within this row.</param>
public record struct NewestModPageVersionData(NexusModsFileMetadata.ReadOnly NewestFile, int NumToUpdate);
