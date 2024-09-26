using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Networking.ModUpdates.Traits;
namespace NexusMods.Networking.ModUpdates.Tests.Helpers;

// Helper class to simulate updateable items
public class TestItem : ICanGetLastUpdatedTimestamp, ICanGetUidForMod
{
    public DateTime LastUpdated { get; set; }
    public UidForMod Uid { get; set; }

    public DateTime GetLastUpdatedDate() => LastUpdated;
    public UidForMod GetUniqueId() => Uid;
    
    // Helper method to create a test item
    public static TestItem Create(uint gameId, uint modId, DateTime lastUpdated)
    {
        return new TestItem
        {
            Uid = new UidForMod { GameId = GameId.From(gameId), ModId = ModId.From(modId) },
            LastUpdated = lastUpdated,
        };
    }
}


