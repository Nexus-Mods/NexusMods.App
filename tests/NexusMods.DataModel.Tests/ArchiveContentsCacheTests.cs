using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Tests;

public class ArchiveContentsCacheTests : ADataModelTest<ArchiveContentsCacheTests>
{
    public ArchiveContentsCacheTests(IServiceProvider provider) : base(provider)
    {
    }


    [Fact]
    public async Task CanAnalyzeArchives()
    {
        var analyzed = (AnalyzedArchive)await ArchiveContentsCache.AnalyzeFile(DATA_ZIP_LZMA, CancellationToken.None);

        analyzed.Contents.Count.Should().Be(3);
        analyzed.Hash.Should().Be((Hash)0x706F72D12A82892DL);
        var file = analyzed.Contents["folder1/folder1file.txt".ToRelativePath()];
        file.Hash.Should().Be((Hash)0xC9E47B1523162066L);


        var result = DataStore.Get<FileContainedIn>(new TwoId64(EntityCategory.FileContainedIn, (ulong)file.Hash, (ulong)analyzed.Hash));
        result.Should().NotBeNull();
        var reverse = ArchiveContentsCache.ArchivesThatContain(file.Hash).ToArray();
        reverse.Length.Should().BeGreaterThan(0);
        reverse.Select(r => r.Parent).Should().Contain(analyzed.Hash);

    }



}