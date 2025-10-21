using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Networking.ModUpdates.Tests.Helpers;

// Helper class to simulate updateable items
public class TestModFeedItem : IModFeedItem
{
    public DateTime LastUpdated { get; set; }
    public UidForMod Uid { get; set; }

    public DateTimeOffset GetLastUpdatedDate() => LastUpdated;
    public UidForMod GetModPageId() => Uid;
    
    // Helper method to create a test item
    public static TestModFeedItem Create(uint gameId, uint modId, DateTime lastUpdated)
    {
        return new TestModFeedItem
        {
            Uid = new UidForMod(ModId.From(modId), GameId.From(gameId)),
            LastUpdated = lastUpdated,
        };
    }
}


