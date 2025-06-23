namespace NexusMods.Sdk.Tests;

public class LightweightLRUCacheTests
{
    [Test]
    public async Task AddedItemsCanBeRetrieved()
    {
        var cache = new LightweightLRUCache<int, int>(2);

        cache.Add(1, 1);
        await Assert.That(cache.Count).IsEqualTo(1);

        cache.Add(2, 2);
        await Assert.That(cache.Count).IsEqualTo(2);
        
        cache.Add(3, 3);
        await Assert.That(cache.Count).IsEqualTo(2).Because("Cache has a size of 2");

        await Assert.That(cache.TryGet(1, out _)).IsFalse().Because("Item with key 3 replaced item with key 1 because of the max size of 2");

        await Assert.That(cache.TryGet(2, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(2);

        await Assert.That(cache.TryGet(3, out value)).IsTrue();
        await Assert.That(value).IsEqualTo(3);
    }

    [Test]
    public async Task DuplicateItemsAreUpdated()
    {
        var cache = new LightweightLRUCache<int, int>(2);

        cache.Add(1, 1);
        await Assert.That(cache.Count).IsEqualTo(1);

        cache.Add(1, 2);
        await Assert.That(cache.Count).IsEqualTo(1).Because("Item with key 1 got updated, no new item was added");

        await Assert.That(cache.TryGet(1, out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(2).Because("Item with key 1 got updated from value 1 to value 2");
    }
}
