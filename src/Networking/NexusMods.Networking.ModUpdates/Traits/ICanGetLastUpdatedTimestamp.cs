namespace NexusMods.Networking.ModUpdates.Traits;

/// <summary>
/// This interface marks an item which has the time it was last updated.
/// </summary>
public interface ICanGetLastUpdatedTimestamp
{
    /// <summary>
    /// Retrieves the time the item was last updated.
    /// This date is in UTC.
    /// </summary>
    public DateTime GetLastUpdatedDate();
}
