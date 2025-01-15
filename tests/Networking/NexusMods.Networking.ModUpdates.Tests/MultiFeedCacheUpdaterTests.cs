using FluentAssertions;
using NexusMods.Networking.ModUpdates.Tests.Helpers;
using static NexusMods.Networking.ModUpdates.Tests.Helpers.TestModFeedItem;

namespace NexusMods.Networking.ModUpdates.Tests;

public class MultiFeedCacheUpdaterTests
{
    [Fact]
    public void Constructor_WithEmptyItems_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new MultiFeedCacheUpdater<TestModFeedItem>([], TimeSpan.FromDays(30));

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldSetOldItemsToNeedUpdate()
    {
        // Items which have a 'last checked date' older than expiry
        // should be marked as 'Out of Date' across multiple feeds.
        
        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-40)),
            Create(1, 2, now.AddDays(-20)),
            Create(2, 1, now.AddDays(-35)),
            Create(2, 2, now.AddDays(-25)),
        };

        // Act
        var updater = new MultiFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));
        var result = updater.BuildFlattened();

        // Assert
        // items [0] and [2] should be out of date 
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[0]);
        result.OutOfDateItems.Should().Contain(items[2]);
    }

    [Fact]
    public void Update_ShouldMarkMissingItemsAsUndetermined()
    {
        // Items that are missing from the 'updated' payload
        // may be 'inaccessible', such as deleted or taken down
        // due to a DMCA. We should notice the mods which fall
        // under this category across multiple feeds.

        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
            Create(2, 1, now.AddDays(-15)),
            Create(2, 2, now.AddDays(-8)),
        };

        var updater = new MultiFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));

        var updateItems = new[]
        {
            Create(1, 1, now.AddDays(-8)),  // Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Older, up-to-date
            Create(2, 1, now.AddDays(-13)), // Newer, needs update
        };

        // Act
        updater.Update(updateItems);
        var result = updater.BuildFlattened();

        // Assert
        // item[3] is not in 'update' feed/result, but is within
        // the expiry date. This means the mod page may have been archived
        // or taken down due to a DMCA.
        result.UndeterminedItems.Should().ContainSingle();
        result.UndeterminedItems.Should().Contain(items[3]);
    }

    [Fact]
    public void Update_ShouldMarkItemsAsUpToDateOrNeedingUpdateAcrossMultipleFeeds()
    {
        // The MultiFeedCacheUpdater correctly compares the 'lastUpdated' field
        // of the update entry with the 'lastUpdated' field of the cached item
        // across multiple feeds.
        
        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
            Create(2, 1, now.AddDays(-12)),
            Create(2, 2, now.AddDays(-7)),
        };

        var updater = new MultiFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));
        var updateItems = new[]
        {
            Create(1, 1, now.AddDays(-8)),  // Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Older, up-to-date
            Create(2, 1, now.AddDays(-10)), // Newer, needs update
            Create(2, 2, now.AddDays(-9)),  // Older, up-to-date
        };

        // Act
        updater.Update(updateItems);
        var result = updater.BuildFlattened();

        // Assert
        // items[0] and items[2] are out of date, items[1] and items[3] are up-to-date
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[0]);
        result.OutOfDateItems.Should().Contain(items[2]);

        result.UpToDateItems.Should().HaveCount(2);
        result.UpToDateItems.Should().Contain(items[1]);
        result.UpToDateItems.Should().Contain(items[3]);
    }
    
    [Fact]
    public void Update_HavingExtraItemsInUpdatePayloadHasNoSideEffects()
    {
        // Copy of Update_ShouldMarkItemsAsUpToDateOrNeedingUpdateAcrossMultipleFeeds, 
        // but with extra item(s) in update payload. These items are ones we don't have cached;
        // therefore they should be ignored without side effects.

        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
            Create(2, 1, now.AddDays(-12)),
            Create(2, 2, now.AddDays(-7)),
        };

        var updater = new MultiFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));

        var updateItems = new[]
        {
            Create(1, 5, now),               // New item, should be ignored
            Create(2, 6, now),               // New item, should be ignored
            Create(1, 1, now.AddDays(-8)),   // Newer, needs update
            Create(1, 2, now.AddDays(-7)),   // Older, up-to-date
            Create(2, 1, now.AddDays(-10)),  // Newer, needs update
            Create(2, 2, now.AddDays(-9)),   // Older, up-to-date
            Create(1, 7, now),               // New item, should be ignored
            Create(2, 8, now),               // New item, should be ignored
        };

        // Act
        updater.Update(updateItems);
        var result = updater.BuildFlattened();

        // Assert
        // items[0] and items[2] are out of date, items[1] and items[3] are up-to-date
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[0]);
        result.OutOfDateItems.Should().Contain(items[2]);

        result.UpToDateItems.Should().HaveCount(2);
        result.UpToDateItems.Should().Contain(items[1]);
        result.UpToDateItems.Should().Contain(items[3]);
    }

    [Fact]
    public void Update_ShouldHandleUpdatesFromDifferentFeeds()
    {
        // Ensure that the MultiFeedCacheUpdater correctly handles updates
        // from different feeds separately.

        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
            Create(2, 1, now.AddDays(-12)),
            Create(2, 2, now.AddDays(-7)),
            Create(3, 1, now.AddDays(-15)),
        };

        var updater = new MultiFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));

        var updateItems = new[]
        {
            Create(1, 1, now.AddDays(-8)),  // Feed 1: Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Feed 1: Older, up-to-date
            Create(2, 1, now.AddDays(-10)), // Feed 2: Newer, needs update
            Create(3, 1, now.AddDays(-16)), // Feed 3: Older, up-to-date
        };

        // Act
        updater.Update(updateItems);
        var result = updater.BuildFlattened();

        // Assert
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[0]);
        result.OutOfDateItems.Should().Contain(items[2]);

        result.UpToDateItems.Should().HaveCount(2);
        result.UpToDateItems.Should().Contain(items[1]);
        result.UpToDateItems.Should().Contain(items[4]);

        result.UndeterminedItems.Should().ContainSingle();
        result.UndeterminedItems.Should().Contain(items[3]);
    }
}
