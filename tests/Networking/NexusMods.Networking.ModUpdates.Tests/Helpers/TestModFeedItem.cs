using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.ModUpdates.Tests.Helpers;

// Helper class to simulate updateable items
public class TestModFeedItem : IModFeedItem
{
    public DateTime LastUpdated { get; set; }
    public ModUid Uid { get; set; }

    public DateTimeOffset GetLastUpdatedDate() => LastUpdated;
    public ModUid GetModPageId() => Uid;
    
    // Helper method to create a test item
    public static TestModFeedItem Create(uint gameId, uint modId, DateTime lastUpdated)
    {
        return new TestModFeedItem
        {
            Uid = new ModUid(ModId.From(modId), NexusModsGameId.From(gameId)),
            LastUpdated = lastUpdated,
        };
    }
}


