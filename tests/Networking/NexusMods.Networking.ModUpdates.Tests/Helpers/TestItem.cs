using NexusMods.Networking.ModUpdates.Structures;
using NexusMods.Networking.ModUpdates.Traits;
namespace NexusMods.Networking.ModUpdates.Tests;

// Helper class to simulate updateable items
public class TestItem : ICanGetLastUpdatedTimestamp, ICanGetUid
{
    public DateTime LastUpdated { get; set; }
    public Uid Uid { get; set; }

    public DateTime GetLastUpdatedDate() => LastUpdated;
    public Uid GetUniqueId() => Uid;
    
    // Helper method to create a test item
    public static TestItem Create(uint gameId, uint modId, DateTime lastUpdated)
    {
        return new TestItem
        {
            Uid = new Uid { GameId = GameId.From(gameId), ModId = ModId.From(modId) },
            LastUpdated = lastUpdated,
        };
    }
}


