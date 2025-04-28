using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// Represents an individual item from a 'mod feed'; with the 'mod feed' being
/// the result of an API call that returns one or more mods from the Nexus API.
/// (Either V1 or V2 API)
/// </summary>
public interface IModFeedItem
{
    /// <summary>
    /// Returns a unique identifier for the given item, based on the ID format
    /// used in the NexusMods V2 API.
    /// </summary>
    public UidForMod GetModPageId();
    
    /// <summary>
    /// Retrieves the time the item was last updated.
    /// This date is in UTC.
    /// </summary>
    public DateTimeOffset GetLastUpdatedDate();
}
