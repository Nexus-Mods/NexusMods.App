using System.Globalization;
using FluentAssertions;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Paths.Tests.FileSystem;

public class BaseFileSystemTests
{
    [Theory, AutoFileSystem]
    public void Test_PathMapping(InMemoryFileSystem fs, AbsolutePath originalPath, AbsolutePath mappedPath)
    {
        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(new Dictionary<AbsolutePath, AbsolutePath>
        {
            { originalPath, mappedPath }
        }, new Dictionary<KnownPath, AbsolutePath>());

        overlayFileSystem.GetMappedPath(originalPath).Should().Be(mappedPath);
    }

    [Theory, AutoFileSystem]
    public void Test_PathMapping_WithDirectory(InMemoryFileSystem fs,
        AbsolutePath originalDirectoryPath, AbsolutePath newDirectoryPath, string fileName)
    {
        var originalFilePath = originalDirectoryPath.CombineUnchecked(fileName);
        var newFilePath = newDirectoryPath.CombineUnchecked(fileName);

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>
            {
                { originalDirectoryPath, newDirectoryPath }
            },
            new Dictionary<KnownPath, AbsolutePath>());

        overlayFileSystem.GetMappedPath(originalFilePath).Should().Be(newFilePath);
    }

    [SkippableTheory, AutoFileSystem]
    public async Task Test_ReadAllBytesAsync(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.AddFile(path, contents);
        var result = await fs.ReadAllBytesAsync(path);
        result.Should().BeEquivalentTo(contents);
    }

    [SkippableTheory, AutoFileSystem]
    public async Task Test_ReadAllTextAsync(InMemoryFileSystem fs, AbsolutePath path, string contents)
    {
        fs.AddFile(path, contents);
        var result = await fs.ReadAllTextAsync(path);
        result.Should().BeEquivalentTo(contents);
    }

    [SkippableTheory]
    [InlineData("C:\\", "/c")]
    [InlineData("C:\\foo\\bar", "/c/foo/bar")]
    public void Test_ConvertCrossPlatformPath(string input, string output)
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var fs = new InMemoryFileSystem().CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>(),
            new Dictionary<KnownPath, AbsolutePath>(),
            true);

        var path = fs.FromFullPath(input);
        path.GetFullPath().Should().Be(output);
    }

    [SkippableTheory, AutoFileSystem]
    public void Test_KnownPathMappings(IFileSystem fs)
    {
        Skip.IfNot(OperatingSystem.IsLinux());

        var knownPathMappings = new Dictionary<KnownPath, AbsolutePath>();
        var values = Enum.GetValues<KnownPath>();
        foreach (var knownPath in values)
        {
            var newPath = fs.FromFullPath($"/{Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)}");
            knownPathMappings[knownPath] = newPath;
        }

        var overlayFileSystem = fs.CreateOverlayFileSystem(new Dictionary<AbsolutePath, AbsolutePath>(), knownPathMappings);
        foreach (var knownPath in values)
        {
            var actualPath = overlayFileSystem.GetKnownPath(knownPath);
            var expectedPath = knownPathMappings[knownPath];

            actualPath.Should().Be(expectedPath);
        }
    }
}
