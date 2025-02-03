using System.Diagnostics;
using FluentAssertions;
using NexusMods.Networking.ModUpdates.Tests.Helpers;
using static NexusMods.Networking.ModUpdates.Tests.Helpers.TestModFeedItem;

namespace NexusMods.Networking.ModUpdates.Tests;

public class PerFeedCacheUpdaterTests
{
    [Fact]
    public void Constructor_WithEmptyItems_ShouldNotThrow()
    {
        // Arrange & Act
        Action act = () => new PerFeedCacheUpdater<TestModFeedItem>([], TimeSpan.FromDays(30));

        // Assert
        act.Should().NotThrow();
    }

    #if DEBUG
    [Fact]
    // [Conditional("DEBUG")] 👈 this doesn't work right, sadly, so we do debug if.
    public void Constructor_WithItemsFromDifferentGames_ShouldThrowArgumentException_InDebug()
    {
        // Arrange
        var items = new[]
        {
            Create(1, 1, DateTime.UtcNow),
            Create(2, 1, DateTime.UtcNow),
        };

        // Act
        // ReSharper disable once HeapView.ObjectAllocation.Evident
        // ReSharper disable once ObjectCreationAsStatement
        Action act = () => new PerFeedCacheUpdater<TestModFeedItem>(items, TimeSpan.FromDays(30));

        // Assert
        act.Should().Throw<ArgumentException>();
    }
    #endif

    [Fact]
    public void Constructor_ShouldSetOldItemsToNeedUpdate()
    {
        // Items which have a 'last checked date' older than expiry
        // should be marked as 'Out of Date'.
        
        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-40)),
            Create(1, 2, now.AddDays(-20)),
            Create(1, 3, now.AddDays(-35)),
        };

        // Act
        var expiry = TimeSpan.FromDays(30);
        var updater = new PerFeedCacheUpdater<TestModFeedItem>(items, expiry);
        var result = updater.Build(expiry);

        // Assert
        // input items [0] and [2] should be out of date 
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[0]);
        result.OutOfDateItems.Should().Contain(items[2]);
    }

    [Fact]
    public void Update_ShouldCorrectlyHandleMissingItems()
    {
        // Items that are missing from the 'updated' payload
        // may be 'inaccessible', such as deleted or taken down
        // due to a DMCA. We should notice the mods which fall
        // under this category. Items that have not been updated
        // within the expiry date are marked as out of date, items
        // updated within expiry are up to date.
        
        // A more robust mechanism to detach may be derived later,
        // but this situation is rare in practice as Nexus does not
        // delete mod pages outside very special reasons

        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
            Create(1, 3, now.AddDays(-31)), // cached, but out of date, is undetermined
            Create(1, 4, now.AddDays(-15)), // cached, but in date, we can keep it.
        };

        var expiry = TimeSpan.FromDays(30);
        var updater = new PerFeedCacheUpdater<TestModFeedItem>(items, expiry);

        var updateItems = new[]
        {
            Create(1, 1, now.AddDays(-8)),  // Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Older, up-to-date
        };

        // Act
        updater.Update(updateItems);
        var result = updater.Build(expiry);

        // Assert
        // item[2] is not in 'update' feed/result, but is within
        // the expiry date. This means the mod page may have been archived
        // or taken down due to a DMCA.
        result.OutOfDateItems.Should().HaveCount(2);
        result.OutOfDateItems.Should().Contain(items[2]);

        result.UpToDateItems.Should().HaveCount(2);
        result.UpToDateItems.Should().Contain(items[3]);
    }

    [Fact]
    public void Update_ShouldMarkItemsAsUpToDateOrNeedingUpdate()
    {
        // The PerFeedCacheUpdater correctly compares the 'lastUpdated' field
        // of the update entry with the 'lastUpdated' field of the cached item.
        // Here we test whether the update function correctly detects if an item
        // is older or newer.
        
        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
        };

        var expiry = TimeSpan.FromDays(30);
        var updater = new PerFeedCacheUpdater<TestModFeedItem>(items, expiry);
        var updateItems = new[]
        {
            Create(1, 1, now.AddDays(-8)),  // Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Older, up-to-date
        };

        // Act
        updater.Update(updateItems);
        var result = updater.Build(expiry);

        // Assert
        // item[0] is out of date, item[1] is up-to-date
        result.OutOfDateItems.Should().ContainSingle();
        result.OutOfDateItems.Should().Contain(items[0]);

        result.UpToDateItems.Should().ContainSingle();
        result.UpToDateItems.Should().Contain(items[1]);
    }
    
    [Fact]
    public void Update_HavingExtraItemsInUpdatePayloadHasNoSideEffects()
    {
        // Copy of Update_ShouldMarkItemsAsUpToDateOrNeedingUpdate, but with extra 
        // item(s) in update payload. These items are ones we don't have cached;
        // therefore they should be ignored without side effects.

        // Arrange
        var now = DateTime.UtcNow;
        var items = new[]
        {
            Create(1, 1, now.AddDays(-10)),
            Create(1, 2, now.AddDays(-5)),
        };

        var expiry = TimeSpan.FromDays(30);
        var updater = new PerFeedCacheUpdater<TestModFeedItem>(items, expiry);

        var updateItems = new[]
        {
            Create(1, 5, now),                        // New item, should be ignored
            Create(1, 6, now),                        // New item, should be ignored
            Create(1, 1, now.AddDays(-8)),  // Newer, needs update
            Create(1, 2, now.AddDays(-7)),  // Older, up-to-date
            Create(1, 4, now),                        // New item, should be ignored
            Create(1, 7, now),                        // New item, should be ignored
        };

        // Act
        updater.Update(updateItems);
        var result = updater.Build(expiry);

        // Assert
        // item[0] is out of date, item[1] is up-to-date
        result.OutOfDateItems.Should().ContainSingle();
        result.OutOfDateItems.Should().Contain(items[0]);

        result.UpToDateItems.Should().HaveCount(1);
        result.UpToDateItems.Should().Contain(items[1]);
    }
}
