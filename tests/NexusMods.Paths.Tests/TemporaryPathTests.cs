using FluentAssertions;

namespace NexusMods.Paths.Tests;

public class TemporaryPathTests : IDisposable
{
    private readonly TemporaryFileManager _manager;

    public TemporaryPathTests()
    {
        _manager = new TemporaryFileManager("baseTmp".ToAbsolutePath().Combine(Guid.NewGuid()));
    }
    
    [Fact]
    public async Task FilesAreInBaseDirectory()
    {
        await using var path = _manager.CreateFile();
        path.Path.Parent.Parent.FileName.Should().Be("baseTmp".ToRelativePath());
    }

    [Fact]
    public async Task FilesAreDeletedWhenFlagged()
    {
        var deletedPath = _manager.CreateFile();
        await deletedPath.Path.WriteAllTextAsync("File A");

        var notDeletedPath = _manager.CreateFile(deleteOnDispose:false);
        await notDeletedPath.Path.WriteAllTextAsync("File B");

        deletedPath.Path.FileExists.Should().BeTrue();
        notDeletedPath.Path.FileExists.Should().BeTrue();
        
        await deletedPath.DisposeAsync();
        await notDeletedPath.DisposeAsync();

        deletedPath.Path.FileExists.Should().BeFalse();
        notDeletedPath.Path.FileExists.Should().BeTrue();
    }

    public void Dispose()
    {
        _manager.Dispose();
    }
}