using FluentAssertions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class FileCacheTests
{
    private readonly TemporaryFileManager _fileManager;
    private readonly FileCache _cache;

    public FileCacheTests(FileCache cache, TemporaryFileManager fileManager)
    {
        _cache = cache;
        _fileManager = fileManager;
    }

    [Fact]
    public async Task CanCacheAndRetrieveFiles()
    {
        await using var file = _fileManager.CreateFile();

        {
            await using var entry = await _cache.Create();
            await entry.Path.WriteAllTextAsync("Hello World!");
        }
        await using var strm = await _cache.Read(Hash.From(0xA52B286A3E7F4D91));
        strm.Should().NotBeNull();
        (await strm!.ReadAllTextAsync()).Should().Be("Hello World!");
        
        await using var path = _fileManager.CreateFile();
        await _cache.CopyTo(Hash.From(0xA52B286A3E7F4D91), path.Path);
        (await path.Path.ReadAllTextAsync()).Should().Be("Hello World!");
    }
}