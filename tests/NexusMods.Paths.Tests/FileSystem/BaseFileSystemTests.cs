using System.Globalization;
using System.Runtime.InteropServices;
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
        var originalFilePath = originalDirectoryPath.Combine(fileName);
        var newFilePath = newDirectoryPath.Combine(fileName);

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>
            {
                { originalDirectoryPath, newDirectoryPath }
            },
            new Dictionary<KnownPath, AbsolutePath>());

        overlayFileSystem.GetMappedPath(originalFilePath).Should().Be(newFilePath);
    }

    [Fact]
    public void Test_PathMappings_SpecialCases()
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeUnix);

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>
            {
                { fs.FromUnsanitizedFullPath("/c"), fs.FromUnsanitizedFullPath("/foo") },
                { fs.FromUnsanitizedFullPath("/z"), fs.FromUnsanitizedFullPath("/") },
            },
            new Dictionary<KnownPath, AbsolutePath>());

        overlayFileSystem.GetMappedPath(fs.FromUnsanitizedFullPath("/c/a")).Should().Be(fs.FromUnsanitizedFullPath("/foo/a"));
        overlayFileSystem.GetMappedPath(fs.FromUnsanitizedFullPath("/z/a")).Should().Be(fs.FromUnsanitizedFullPath("/a"));
    }

    [Theory, AutoFileSystem]
    public async Task Test_ReadAllBytesAsync(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.AddFile(path, contents);
        var result = await fs.ReadAllBytesAsync(path);
        result.Should().BeEquivalentTo(contents);
    }

    [Theory, AutoFileSystem]
    public async Task Test_ReadAllTextAsync(InMemoryFileSystem fs, AbsolutePath path, string contents)
    {
        fs.AddFile(path, contents);
        var result = await fs.ReadAllTextAsync(path);
        result.Should().BeEquivalentTo(contents);
    }

    [Theory]
    [InlineData("C:/", "/c")]
    [InlineData("C:/foo/bar", "/c/foo/bar")]
    public void Test_ConvertCrossPlatformPath(string input, string output)
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeUnix)
            .CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>(),
            new Dictionary<KnownPath, AbsolutePath>(),
            true);

        var path = fs.FromUnsanitizedFullPath(input);
        path.GetFullPath().Should().Be(output);
    }

    [Fact]
    public void Test_KnownPathMappings()
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeUnix);

        var knownPathMappings = new Dictionary<KnownPath, AbsolutePath>();
        var values = Enum.GetValues<KnownPath>();
        foreach (var knownPath in values)
        {
            var newPath = fs.FromUnsanitizedFullPath($"/{Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture)}");
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

    [Fact]
    public void Test_EnumerateRootDirectories_Windows()
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeWindows);

        var rootDirectory = fs.FromUnsanitizedFullPath("C:/");
        var pathMappings = Enumerable.Range('A', 'Z' - 'A')
            .Select(iDriveLetter =>
            {
                var driveLetter = (char)iDriveLetter;
                var originalPath = fs.FromUnsanitizedFullPath($"{driveLetter}:/");
                var newPath = rootDirectory.Combine(Guid.NewGuid().ToString("D"));
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

    [Fact]
    public void Test_EnumerateRootDirectories_Linux()
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeUnix);

        var rootDirectory = fs.FromUnsanitizedFullPath("/");
        var expectedRootDirectories = new[] { rootDirectory };
        var actualRootDirectories = fs
            .EnumerateRootDirectories()
            .ToArray();

        actualRootDirectories.Should().BeEquivalentTo(expectedRootDirectories);
    }

    [Fact]
    public void Test_EnumerateRootDirectories_WithCrossPlatformPathMappings()
    {
        var fs = new InMemoryFileSystem(OSInformation.FakeUnix);

        var rootDirectory = fs.FromUnsanitizedFullPath("/");

        var pathMappings = Enumerable.Range('a', 'z' - 'a')
            .Select(iDriveLetter =>
            {
                var driveLetter = (char)iDriveLetter;
                var originalPath = fs.FromUnsanitizedDirectoryAndFileName("/", driveLetter.ToString());
                var newPath = rootDirectory.Combine(Guid.NewGuid().ToString("D"));
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

    [SkippableTheory]
    [InlineData(true, KnownPath.EntryDirectory, true)]
    [InlineData(true, KnownPath.CurrentDirectory, true)]
    [InlineData(true, KnownPath.CommonApplicationDataDirectory, true)]
    [InlineData(true, KnownPath.ProgramFilesDirectory, false)]
    [InlineData(true, KnownPath.ProgramFilesX86Directory, false)]
    [InlineData(true, KnownPath.CommonProgramFilesDirectory, false)]
    [InlineData(true, KnownPath.CommonProgramFilesX86Directory, false)]
    [InlineData(true, KnownPath.TempDirectory, true)]
    [InlineData(true, KnownPath.HomeDirectory, true)]
    [InlineData(true, KnownPath.ApplicationDataDirectory, true)]
    [InlineData(true, KnownPath.LocalApplicationDataDirectory, true)]
    [InlineData(true, KnownPath.MyDocumentsDirectory, true)]
    [InlineData(true, KnownPath.MyGamesDirectory, true)]

    [InlineData(false, KnownPath.EntryDirectory, true)]
    [InlineData(false, KnownPath.CurrentDirectory, true)]
    [InlineData(false, KnownPath.CommonApplicationDataDirectory, true)]
    [InlineData(false, KnownPath.ProgramFilesDirectory, true)]
    [InlineData(false, KnownPath.ProgramFilesX86Directory, true)]
    [InlineData(false, KnownPath.CommonProgramFilesDirectory, true)]
    [InlineData(false, KnownPath.CommonProgramFilesX86Directory, true)]
    [InlineData(false, KnownPath.TempDirectory, true)]
    [InlineData(false, KnownPath.HomeDirectory, true)]
    [InlineData(false, KnownPath.ApplicationDataDirectory, true)]
    [InlineData(false, KnownPath.LocalApplicationDataDirectory, true)]
    [InlineData(false, KnownPath.MyDocumentsDirectory, true)]
    [InlineData(false, KnownPath.MyGamesDirectory, true)]
    public void Test_KnownPath(bool isLinux, KnownPath knownPath, bool expected)
    {
        Skip.If(isLinux != OSInformation.Shared.IsLinux);
        var fs = new InMemoryFileSystem();

        var actual = fs.HasKnownPath(knownPath);
        actual.Should().Be(expected);

        Action act = () => fs.GetKnownPath(knownPath);
        if (expected) act.Should().NotThrow();
        else act.Should().Throw<Exception>();
    }
}
