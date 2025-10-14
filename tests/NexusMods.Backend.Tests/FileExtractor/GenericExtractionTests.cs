using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;

namespace NexusMods.Backend.Tests.FileExtractor;

public class GenericExtractionTests : AFileExtractorTest
{
    [Test]
    [ArchiveDataSource]
    public async Task CanExtractAll(AbsolutePath path)
    {
        await using var tempFolder = TemporaryFileManager.CreateFolder();
        await FileExtractor.ExtractAllAsync(path, tempFolder, CancellationToken.None);

        var actual = await tempFolder.Path.EnumerateFiles()
            .ToAsyncEnumerable()
            .SelectAwait(async f => (f.RelativeTo(tempFolder.Path), await f.XxHash3Async()))
            .ToArrayAsync();

        (RelativePath, Hash)[] expected = [
            ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt", (Hash)0x3F0AB4D495E35A9A),
            ("folder1/folder1file.txt", (Hash)0x8520436F06348939),
            ("rootFile.txt", (Hash)0x818A82701BC1CC30),
        ];

        actual.Should().BeEquivalentTo(expected);
    }
}

file class ArchiveDataSource : DataSourceGeneratorAttribute<AbsolutePath>
{
    protected override IEnumerable<Func<AbsolutePath>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources")
            .EnumerateFiles()
            .Where(file => file.FileName is "data_7zip_lzma2.7z" or "data_zip_lzma.zip")
            .Select<AbsolutePath, Func<AbsolutePath>>(file => () => file);
    }
}
