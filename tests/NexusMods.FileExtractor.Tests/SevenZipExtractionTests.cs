using FluentAssertions;
using NexusMods.Abstractions.FileExtractor;
using NexusMods.Extensions.BCL;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using Reloaded.Memory.Extensions;

namespace NexusMods.FileExtractor.Tests;

public class SevenZipExtractionTests
{
    private readonly IFileExtractor _extractor;

    private readonly TemporaryFileManager _temporaryFileManager;

    private readonly IFileSystem _fileSystem;

    public SevenZipExtractionTests(IFileExtractor extractor, TemporaryFileManager temporaryFileManager,
        IFileSystem fileSystem)
    {
        _extractor = extractor;
        _temporaryFileManager = temporaryFileManager;
        _fileSystem = fileSystem;
    }

    [Theory]
    [InlineData("foo/bar", "foo/bar")]
    [InlineData("foo/bar ", "foo/bar_")]
    [InlineData("foo/bar  ", "foo/bar__")]
    [InlineData("foo/bar.", "foo/bar_")]
    [InlineData("foo/bar..", "foo/bar__")]
    [InlineData("foo/bar. ", "foo/bar__")]
    public void Test_To7ZipWindowsExtractionPath(string input, string expected)
    {
        Span<char> span = stackalloc char[input.Length];
        input.AsSpan().CopyTo(span);

        var slice = span.SliceFast(start: 0, length: input.Length);
        SevenZipExtractor.To7ZipWindowsExtractionPath(slice);

        var actual = slice.ToString();
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task CanExtractToLongPath()
    {
        await using var tempFolder = _temporaryFileManager.CreateFolder();
        var dest = tempFolder.Path;

        // Create a long path
        while (!(dest.GetFullPathLength() > 280))
        {
            dest = dest.Combine("subfolder");
            _fileSystem.CreateDirectory(dest);
        }

        dest.GetFullPathLength().Should().BeGreaterThan(280);

        _fileSystem.CreateDirectory(dest);

        var file = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory)
            .Combine("Resources/data_7zip_lzma2.7z");

        var act = async () => await _extractor.ExtractAllAsync(file, dest, CancellationToken.None);

        await act.Should().NotThrowAsync();


        (await tempFolder.Path.EnumerateFiles()
                .SelectAsync(async f => (f.RelativeTo(dest), await f.XxHash3Async()))
                .ToArrayAsync())
            .Should()
            .BeEquivalentTo(new[]
            {
                ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt".ToRelativePath(), (Hash)0x3F0AB4D495E35A9A),
                ("folder1/folder1file.txt".ToRelativePath(), (Hash)0x8520436F06348939),
                ("rootFile.txt".ToRelativePath(), (Hash)0x818A82701BC1CC30),
            });


    }
}
