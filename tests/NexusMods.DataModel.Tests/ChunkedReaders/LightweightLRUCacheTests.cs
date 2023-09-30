using FluentAssertions;
using NexusMods.DataModel.ChunkedStreams;

namespace NexusMods.DataModel.Tests.ChunkedReaders;

public class LightweightLRUCacheTests
{
    [Fact]
    public void AddedItemsCanBeRetrieved()
    {
        var cache = new LightweightLRUCache<int, int>(2);
        cache.Add(1, 1);
        cache.Count.Should().Be(1);
        cache.Add(2, 2);
        cache.Count.Should().Be(2);
        cache.Add(3, 3);
        cache.Count.Should().Be(2);

        cache.TryGet(1, out var value).Should().BeFalse();
        cache.TryGet(2, out value).Should().BeTrue();
        value.Should().Be(2);

        cache.TryGet(3, out value).Should().BeTrue();
        value.Should().Be(3);
    }

    [Fact]
    public void DuplicateItemsAreUpdated()
    {
        var cache = new LightweightLRUCache<int, int>(2);
        cache.Add(1, 1);
        cache.Count.Should().Be(1);
        cache.Add(1, 2);
        cache.Count.Should().Be(1);

        cache.TryGet(1, out var value).Should().BeTrue();
        value.Should().Be(2);
    }

}
