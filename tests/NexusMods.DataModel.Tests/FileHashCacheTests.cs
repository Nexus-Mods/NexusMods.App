using FluentAssertions;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class FileHashCacheTests : ADataModelTest<FileHashCacheTests>
{
    public FileHashCacheTests(IServiceProvider provider) : base(provider)
    {
    }
    
    [Fact]
    public async Task CanGetHashOfSingleFile()
    {
        var file = KnownFolders.CurrentDirectory.Join(Guid.NewGuid().ToString()).WithExtension(Ext.Tmp);
        await file.WriteAllTextAsync("Test data here");

        var hash = await FileHashCache.HashFileAsync(file);
        hash.Hash.Should().Be(0xB08C91D1CDF11402);
        FileHashCache.TryGetCached(file, out var found).Should().BeTrue();
        found.Hash.Should().Be(hash.Hash);
        file.Delete();
    }

    [Fact]
    public async Task CanHashFolder()
    {
        var folder = KnownFolders.CurrentDirectory.Join("tempData");
        var file = folder.Join(Guid.NewGuid().ToString()).WithExtension(Ext.Tmp);
        file.Parent.CreateDirectory();
        await file.WriteAllTextAsync("Test data here");

        var results = await FileHashCache.IndexFolder(folder, CancellationToken.None).ToList();
        results.Should().ContainSingle(x => x.Path == file);
    }


}