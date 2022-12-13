using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.Paths;

namespace NexusMods.DataModel.Tests;

public class ArchiveContentsCacheTests
{
    private readonly ArchiveContentsCache _cache;
    private readonly AbsolutePath _zipFile;
    private readonly IDataStore _store;

    public ArchiveContentsCacheTests(ArchiveContentsCache cache, IDataStore store)
    {
        _cache = cache;
        _store = store;
        _zipFile = KnownFolders.EntryFolder.Combine(@"Resources\data_zip_lzma.zip");
    }


    [Fact]
    public async Task CanAnalyzeArchives()
    {
        var analyzed = (AnalyzedArchive)await _cache.AnalyzeFile(_zipFile, CancellationToken.None);

        analyzed.Contents.Count.Should().Be(3);
        analyzed.Hash.Should().Be(0x706F72D12A82892DL);
        var file = analyzed.Contents["folder1/folder1file.txt".ToRelativePath()];
        file.Hash.Should().Be(0xC9E47B1523162066L);


        var result = _store.Get<FileContainedIn>(new TwoId64(EntityCategory.FileContainedIn, file.Hash, analyzed.Hash));
        result.Should().NotBeNull();
        var reverse = _cache.ArchivesThatContain(file.Hash).ToArray();
        reverse.Length.Should().BeGreaterThan(0);
        reverse.Select(r => r.Parent).Should().Contain(analyzed.Hash);

    }
    
    
    
}