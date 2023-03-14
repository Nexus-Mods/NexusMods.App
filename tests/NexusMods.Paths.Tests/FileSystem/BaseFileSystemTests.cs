using AutoFixture.Xunit2;
using FluentAssertions;
using NexusMods.Paths.Tests.AutoData;

namespace NexusMods.Paths.Tests.FileSystem;

public class BaseFileSystemTests
{
    [Theory, AutoFileSystem]
    public void Test_PathMapping(InMemoryFileSystem fs, AbsolutePath originalPath, AbsolutePath mappedPath)
    {
        var overlayFileSystem = (BaseFileSystem)fs.CreateOverlayFileSystem(new Dictionary<AbsolutePath, AbsolutePath>
        {
            { originalPath, mappedPath }
        });

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
            });

        overlayFileSystem.GetMappedPath(originalFilePath).Should().Be(newFilePath);
    }

    [SkippableTheory, AutoFileSystem]
    public async Task Test_ReadAllBytesAsync(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        Skip.IfNot(OperatingSystem.IsLinux());
        fs.AddFile(path, contents);
        var result = await fs.ReadAllBytesAsync(path);
        result.Should().BeEquivalentTo(contents);
    }

    [SkippableTheory, AutoFileSystem]
    public async Task Test_ReadAllTextAsync(InMemoryFileSystem fs, AbsolutePath path, string contents)
    {
        Skip.IfNot(OperatingSystem.IsLinux());
        fs.AddFile(path, contents);
        var result = await fs.ReadAllTextAsync(path);
        result.Should().BeEquivalentTo(contents);
    }
}
