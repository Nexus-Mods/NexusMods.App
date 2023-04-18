using FluentAssertions;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Paths.Tests;

public class TemporaryPathTests
{
    [Theory, AutoFileSystem]
    public void FilesAreInBaseDirectory(InMemoryFileSystem fs, TemporaryFileManager manager)
    {
        using var path = manager.CreateFile();
        path.Path.InFolder(fs.GetKnownPath(KnownPath.TempDirectory)).Should().BeTrue();
    }

    [Theory, AutoFileSystem]
    public async Task FilesAreDeletedWhenFlagged(InMemoryFileSystem fs, TemporaryFileManager manager)
    {
        var deletedPath = manager.CreateFile();
        await fs.WriteAllTextAsync(deletedPath, "File A");

        var notDeletedPath = manager.CreateFile(deleteOnDispose: false);
        await fs.WriteAllTextAsync(notDeletedPath, "File B");

        fs.FileExists(deletedPath).Should().BeTrue();
        fs.FileExists(notDeletedPath).Should().BeTrue();

        await deletedPath.DisposeAsync();
        await notDeletedPath.DisposeAsync();

        fs.FileExists(deletedPath).Should().BeFalse();
        fs.FileExists(notDeletedPath).Should().BeTrue();
    }
}
