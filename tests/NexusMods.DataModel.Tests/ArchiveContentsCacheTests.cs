using FluentAssertions;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Tests.Harness;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.Tests;

public class ArchiveContentsCacheTests : ADataModelTest<ArchiveContentsCacheTests>
{

    public ArchiveContentsCacheTests(IServiceProvider provider) : base(provider)
    {
    }

/* TODO
    [Fact]
    public async Task CanAnalyzeArchives()
    {
        var analyzed = (AnalyzedArchive)await ArchiveAnalyzer.AnalyzeFileAsync(DataZipLzma, CancellationToken.None);

        analyzed.Contents.Count.Should().Be(3);
        analyzed.Hash.Should().Be((Hash)0x706F72D12A82892DL);
        var file = analyzed.Contents["folder1/folder1file.txt".ToRelativePath()];
        file.Hash.Should().Be((Hash)0xC9E47B1523162066L);


        var result = DataStore.Get<FileContainedIn>(new TwoId64(EntityCategory.FileContainedIn, (ulong)file.Hash, (ulong)analyzed.Hash));
        result.Should().NotBeNull();
        var reverse = ArchiveAnalyzer.ArchivesThatContain(file.Hash).ToArray();
        reverse.Length.Should().BeGreaterThan(0);
        reverse.Select(r => r.Parent).Should().Contain(analyzed.Hash);
    }

    [Fact]
    public async Task UpdatingFileAnalyzersRerunsAnalyzers()
    {
        var analyzed = await ArchiveAnalyzer.AnalyzeFileAsync(DataTest, CancellationToken.None);
        var data = analyzed.AnalysisData.OfType<MutatingFileAnalysisData>()
            .FirstOrDefault();
        data.Should().NotBeNull();

        data!.Revision.Should().Be(_revision, "before the file was indexed");
        _revision = 44;
        var oldSig = ArchiveContentsCache.AnalyzersSignature;
        ArchiveContentsCache.RecalculateAnalyzerSignature();
        ArchiveContentsCache.AnalyzersSignature.Should().NotBe(oldSig, "an analyzer changed its revision");


        analyzed = await ArchiveContentsCache.AnalyzeFileAsync(DataTest, CancellationToken.None);
        data = analyzed.AnalysisData.OfType<MutatingFileAnalysisData>()
            .FirstOrDefault();
        data.Should().NotBeNull();
        data!.Revision.Should().Be(_revision, "the file was reindexed");


    }


    private static uint _revision = 42;
    public class MutatingFileAnalyzer : IFileAnalyzer
    {
        public FileAnalyzerId Id => FileAnalyzerId.New("deadbeef-c48e-4929-906f-852b6afecd5e", _revision);
        public IEnumerable<FileType> FileTypes { get; } = new []{FileType.JustTest};

        public async IAsyncEnumerable<IFileAnalysisData> AnalyzeAsync(FileAnalyzerInfo info,
            CancellationToken token = default)
        {
            yield return new MutatingFileAnalysisData()
                { Revision = Id.Revision };
        }
    }

    [JsonName("TEST_MutatingFileAnalysisData")]
    public class MutatingFileAnalysisData : IFileAnalysisData
    {
        public uint Revision { get; set; }
    }
    */
}
