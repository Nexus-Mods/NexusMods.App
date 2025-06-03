using FluentAssertions;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class GenericExtractionTests
{
    private readonly IFileExtractor _extractor;
    private readonly TemporaryFileManager _temporaryFileManager;

    public GenericExtractionTests(IFileExtractor extractor, TemporaryFileManager temporaryFileManager)
    {
        _extractor = extractor;
        _temporaryFileManager = temporaryFileManager;
    }

    [Theory]
    [MemberData(nameof(Archives))]
    public async Task CanExtractAll(AbsolutePath path)
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(path, tempFolder, CancellationToken.None);

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

    public static IEnumerable<object[]> Archives => FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory)
        .Combine("Resources")
        .EnumerateFiles()
        .Where(file => file.FileName is "data_7zip_lzma2.7z" or "data_zip_lzma.zip")
        .Select(file => new object[] { file });
}
