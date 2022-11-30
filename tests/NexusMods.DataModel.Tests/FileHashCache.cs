using FluentAssertions;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class FileHashCacheTests
{
    private readonly FileHashCache _cache;

    public FileHashCacheTests(FileHashCache cache)
    {
        _cache = cache;
    }
    
    [Fact]
    public async Task CanGetHashOfSingleFile()
    {
        var file = KnownFolders.CurrentDirectory.Combine(Guid.NewGuid().ToString()).WithExtension(Ext.Tmp);
        await file.WriteAllTextAsync("Test data here");

        var hash = await _cache.HashFileAsync(file);
        hash.Hash.Should().Be(0xB08C91D1CDF11402);
        _cache.TryGetCached(file, out var found).Should().BeTrue();
        found.Hash.Should().Be(hash.Hash);
    }
}