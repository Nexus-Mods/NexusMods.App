using AutoFixture.Xunit2;
using FluentAssertions;

namespace NexusMods.Paths.Tests.FileSystem;

public class BaseFileSystemTests
{
    [Theory, AutoData]
    public void Test_PathMapping(InMemoryFileSystem fs, string original, string mapped)
    {
        var originalPath = fs.FromFullPath(original);
        var mappedPath = fs.FromFullPath(mapped);

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(new Dictionary<AbsolutePath, AbsolutePath>
        {
            { originalPath, mappedPath }
        });

        overlayFileSystem.GetMappedPath(originalPath).Should().Be(mappedPath);
    }

    [Theory, AutoData]
    public void Test_PathMapping_WithDirectory(InMemoryFileSystem fs, string fileName)
    {
        var originalDirectoryPath = fs.FromFullPath("/foo/bar");
        var newDirectoryPath = fs.FromFullPath("/foo/baz");

        var originalFilePath = originalDirectoryPath.CombineUnchecked(fileName);
        var newFilePath = newDirectoryPath.CombineUnchecked(fileName);

        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(
            new Dictionary<AbsolutePath, AbsolutePath>
            {
                { originalDirectoryPath, newDirectoryPath }
            });

        overlayFileSystem.GetMappedPath(originalFilePath).Should().Be(newFilePath);
    }
}
