using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
namespace NexusMods.Networking.ModUpdates.Traits;

/// <summary>
/// A trait representing an item which has a unique ID.
/// In this case, a 'unique ID' refers to a NexusMods Mod Page ID.
/// 
/// This ID must be truly unique and belong to a specific item of its type.
/// </summary>
public interface ICanGetUid
{
    /// <summary>
    /// Returns a unique identifier for the given item, based on the ID format
    /// used in the NexusMods V2 API.
    /// </summary>
    public UidForMod GetUniqueId();
}
