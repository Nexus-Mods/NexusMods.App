namespace NexusMods.Networking.ModUpdates.Private;

/// <summary>
/// Defines the actions that need to be taken on all elements submitted to the <see cref="PerFeedCacheUpdater{TUpdateableItem}"/>.
/// </summary>
internal enum CacheUpdaterAction : byte
{
    /// <summary>
    /// This defaults to <see cref="UpdateLastCheckedTimestamp"/>.
    /// Either the entry is missing from the remote, or a programmer error has occurred.
    /// </summary>
    Default = 0,
    
    /// <summary>
    /// The item needs to be updated in the local cache.
    /// </summary>
    NeedsUpdate = 1,
    
    /// <summary>
    /// The item's 'last checked timestamp' needs to be updated.
    /// (The item is already up to date)
    /// </summary>
    UpdateLastCheckedTimestamp = 2,
}
