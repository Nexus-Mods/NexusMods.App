using NexusMods.Backend.FileExtractor.Extractors;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.FileExtractor;
using Reloaded.Memory.Extensions;

namespace NexusMods.Backend.Tests.FileExtractor;

public class SevenZipExtractionTests : AFileExtractorTest
{
    [Test]
    public async Task Test_Issue3003()
    {
        const string fileName = "zip-with-spaces.zip";
        var archivePath = FileSystem.Shared.GetKnownPath(KnownPath.CurrentDirectory).Combine("Resources").Combine(fileName);
        await Assert.That(archivePath.FileExists).IsTrue();

        await using var destination = _temporaryFileManager.CreateFolder();
        await _extractor.ExtractAllAsync(archivePath, destination);

        var files = destination.Path.EnumerateFiles().ToArray();
        await Assert.That(files)
            .All()
            .HasProperty(f => f.FileExists)
            .EqualTo(true);
    }

    [Test]
    [Arguments("foo/bar", "foo/bar")]
    [Arguments("foo/bar ", "foo/bar_")]
    [Arguments("foo/bar  ", "foo/bar__")]
    [Arguments("foo/bar.", "foo/bar_")]
    [Arguments("foo/bar..", "foo/bar__")]
    [Arguments("foo/bar. ", "foo/bar__")]
    [Arguments(". ", "__")]
    [Arguments(".", "_")]
    [Arguments(" ", "_")]
    public void Test_To7ZipWindowsExtractionPath(string input, string expected)
    {
        Span<char> span = stackalloc char[input.Length];
        input.AsSpan().CopyTo(span);

        var slice = span.SliceFast(start: 0, length: input.Length);
        SevenZipExtractor.To7ZipWindowsExtractionPath(slice);

        var actual = slice.ToString();
        actual.Should().Be(expected);
    }

    [Test]
    [Arguments("2024-04-16 06:10:44 D....            0            0  .", false, null, true)]
    [Arguments("2024-04-16 06:10:44 D....            0            0  ..", false, null, true)]
    [Arguments("2024-04-16 06:10:44 D....            0            0  foo ", true, "foo ", true)]
    [Arguments("2024-04-16 06:10:44 .....            0            0  foo ", true, "foo ", false)]
    [Arguments("2024-04-16 06:10:44 .....            0            0  foo", false, null, false)]
    [Arguments("2024-04-16 06:10:44 D....            0            0  foo", false, null, true)]
    public void Test_TryParseListCommandOutput(string input, bool expected, string? expectedFileName, bool expectedIsDirectory)
    {
        var actual = SevenZipExtractor.TryParseListCommandOutput(input, out var fileName, out var isDirectory);
        actual.Should().Be(expected);

        fileName.Should().Be(expectedFileName);
        isDirectory.Should().Be(expectedIsDirectory);
    }

    [Test]
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

        var actual = await tempFolder.Path.EnumerateFiles()
            .ToAsyncEnumerable()
            .SelectAwait(async f => (f.RelativeTo(dest), await f.XxHash3Async()))
            .ToArrayAsync();

        (RelativePath, Hash)[] expected = [
            ("deepFolder/deepFolder2/deepFolder3/deepFolder4/deepFile.txt", (Hash)0x3F0AB4D495E35A9A),
            ("folder1/folder1file.txt", (Hash)0x8520436F06348939),
            ("rootFile.txt", (Hash)0x818A82701BC1CC30),
        ];

        actual.Should().BeEquivalentTo(expected);
    }
}
