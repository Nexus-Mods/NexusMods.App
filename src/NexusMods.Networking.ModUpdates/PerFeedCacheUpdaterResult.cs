namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// Stores the result of updating the 'mod page' cache for a given feed or sets
/// of feeds.
/// </summary>
/// <typeparam name="TUpdateableItem">Wrapper for item supported by the cache updater.</typeparam>
public class PerFeedCacheUpdaterResult<TUpdateableItem> where TUpdateableItem : IModFeedItem
{
    /// <summary>
    /// This is a list of items that is 'out of date'.
    /// For these items, we need to fetch updated info from the Nexus Servers and update the timestamp.
    /// </summary>
    public List<TUpdateableItem> OutOfDateItems { get; init; } = new();
    
    /// <summary>
    /// These are the items that are 'up-to-date'.
    /// Just update the timestamp on these items and you're good.
    /// </summary>
    public List<TUpdateableItem> UpToDateItems { get; init; } = new();
    
    /// <summary>
    /// These are the items that are 'undetermined'.
    /// 
    /// These should be treated the same as the items in <see cref="OutOfDateItems"/>;
    /// however having items here is indicative of a possible programmer error.
    /// (Due to inconsistent expiry parameter between Nexus API call and cache updater).
    ///
    /// Consider logging these items.
    /// </summary>
    public List<TUpdateableItem> UndeterminedItems { get; init; } = new();

    /// <summary>
    /// Status of the operation to obtain newer mod pages.
    /// </summary>
    public CacheUpdaterResultStatus ResultStatus { get; init; } = CacheUpdaterResultStatus.Ok;
    
    /// <summary>
    /// Adds items from another <see cref="PerFeedCacheUpdaterResult{TUpdateableItem}"/>
    /// into the current one.
    /// </summary>
    public void AddFrom(PerFeedCacheUpdaterResult<TUpdateableItem> other)
    {
        OutOfDateItems.AddRange(other.OutOfDateItems);
        UpToDateItems.AddRange(other.UpToDateItems);
        UndeterminedItems.AddRange(other.UndeterminedItems);
    }
    
    /// <summary>
    /// Determines if any item in the underlying datastore needs updating.
    /// </summary>
    public bool AnyItemNeedsUpdate() => (OutOfDateItems.Count + UndeterminedItems.Count) > 0;
    
    /// <summary>
    /// Determines if the result is empty.
    /// An empty result indicates that the call to obtain this info was either throttled.
    /// </summary>
    public bool IsEmpty() => OutOfDateItems.Count == 0 && UpToDateItems.Count == 0 && UndeterminedItems.Count == 0;
    
    /// <summary>
    /// Creates a new object with a pre-set status. This is useful if you want to
    /// fail the operation.
    /// </summary>
    public static PerFeedCacheUpdaterResult<TUpdateableItem> WithStatus(CacheUpdaterResultStatus status) => new()
    {
        ResultStatus = status,
    };
}

/// <summary>
/// Status of operation 
/// </summary>
public enum CacheUpdaterResultStatus
{
    /// <summary>
    /// Default status, everything worked.
    /// </summary>
    Ok,
    
    /// <summary>
    /// We didn't obtain this information because the call was throttled due to
    /// built-in App rate limits.
    /// </summary>
    Throttled,
    
    /// <summary>
    /// We didn't obtain this information because the call was throttled due to
    /// website rate limits.
    ///
    /// The result of this method call may contain a partial result, with the
    /// info obtained from before the call was rate limited.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    RateLimited, // Placeholder.
}
