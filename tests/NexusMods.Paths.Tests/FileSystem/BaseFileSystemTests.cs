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
    public void Test_PathMappings_SpecialCases(InMemoryFileSystem fs)
    {
        Skip.IfNot(OSHelper.IsUnixLike());

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>
            {
                { fs.FromFullPath("/c"), fs.FromFullPath("/foo") },
                { fs.FromFullPath("/z"), fs.FromFullPath("/") },
            },
            new Dictionary<KnownPath, AbsolutePath>());

        overlayFileSystem.GetMappedPath(fs.FromFullPath("/c/a")).Should().Be(fs.FromFullPath("/foo/a"));
        overlayFileSystem.GetMappedPath(fs.FromFullPath("/z/a")).Should().Be(fs.FromFullPath("/a"));
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
        Skip.IfNot(OSHelper.IsUnixLike());

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

    [SkippableTheory, AutoFileSystem]
    public void Test_EnumerateRootDirectories_Windows(InMemoryFileSystem fs)
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var rootDirectory = fs.FromFullPath("C:\\");
        var pathMappings = Enumerable.Range('A', 'Z' - 'A')
            .Select(iDriveLetter =>
            {
                var driveLetter = (char)iDriveLetter;
                var originalPath = fs.FromFullPath($"{driveLetter}:\\");
                var newPath = rootDirectory.CombineUnchecked(Guid.NewGuid().ToString("D"));
                return (originalPath, newPath);
            }).ToDictionary(x => x.originalPath, x => x.newPath);

        var overlayFileSystem = fs.CreateOverlayFileSystem(
            pathMappings,
            new Dictionary<KnownPath, AbsolutePath>(),
            convertCrossPlatformPaths: false);

        var expectedRootDirectories = pathMappings
            .Select(kv => kv.Value)
            .ToArray();

        foreach (var expectedRootDirectory in expectedRootDirectories)
        {
            overlayFileSystem.CreateDirectory(expectedRootDirectory);
        }

        var actualRootDirectories = overlayFileSystem
            .EnumerateRootDirectories()
            .ToArray();

        actualRootDirectories.Should().BeEquivalentTo(expectedRootDirectories);
    }

    [SkippableTheory, AutoFileSystem]
    public void Test_EnumerateRootDirectories_UnixLike(InMemoryFileSystem fs)
    {
        Skip.IfNot(OSHelper.IsUnixLike());

        var rootDirectory = fs.FromFullPath("/");
        var expectedRootDirectories = new[] { rootDirectory };
        var actualRootDirectories = fs
            .EnumerateRootDirectories()
            .ToArray();

        actualRootDirectories.Should().BeEquivalentTo(expectedRootDirectories);
    }

    [SkippableTheory, AutoFileSystem]
    public void Test_EnumerateRootDirectories_WithCrossPlatformPathMappings(InMemoryFileSystem fs)
    {
        Skip.IfNot(OSHelper.IsUnixLike());

        var rootDirectory = fs.FromFullPath("/");

        var pathMappings = Enumerable.Range('a', 'z' - 'a')
            .Select(iDriveLetter =>
            {
                var driveLetter = (char)iDriveLetter;
                var originalPath = fs.FromDirectoryAndFileName("/", driveLetter.ToString());
                var newPath = rootDirectory.CombineUnchecked(Guid.NewGuid().ToString("D"));
                return (originalPath, newPath);
            }).ToDictionary(x => x.originalPath, x => x.newPath);

        var overlayFileSystem = fs.CreateOverlayFileSystem(
            pathMappings,
            new Dictionary<KnownPath, AbsolutePath>(),
            convertCrossPlatformPaths: true);

        var expectedRootDirectories = pathMappings
            .Select(kv => kv.Value)
            .Append(rootDirectory)
            .ToArray();

        foreach (var expectedRootDirectory in expectedRootDirectories)
        {
            overlayFileSystem.CreateDirectory(expectedRootDirectory);
        }

        var actualRootDirectories = overlayFileSystem
            .EnumerateRootDirectories()
            .ToArray();

        actualRootDirectories.Should().BeEquivalentTo(expectedRootDirectories);
    }
}
