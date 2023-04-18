using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class PathExpansionTests
{
    private readonly InMemoryFileSystem _fileSystem;

    public PathExpansionTests()
    {
        _fileSystem = new InMemoryFileSystem();
    }
    
    [Fact]
    public void CanExpandEntryFolder()
    {
        var inputPath = "{EntryFolder}/FluffyKitten.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.Combine(_fileSystem.GetKnownPath(KnownPath.EntryDirectory).GetFullPath(), "FluffyKitten.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandCurrentDirectory()
    {
        var inputPath = "{CurrentDirectory}/AdorableWhiskers.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.Combine(_fileSystem.GetKnownPath(KnownPath.CurrentDirectory).GetFullPath(), "AdorableWhiskers.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandHomeFolder()
    {
        var inputPath = "{HomeFolder}/CuddlyFurball.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.Combine(_fileSystem.GetKnownPath(KnownPath.HomeDirectory).GetFullPath(), "CuddlyFurball.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandMyGames()
    {
        var inputPath = "{MyGames}/CuriousWhiskers.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.Combine(_fileSystem.GetKnownPath(KnownPath.MyGamesDirectory).GetFullPath(), "CuriousWhiskers.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandPath_WithNoPlaceholder()
    {
        var inputPath = "AdorableMeow.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.GetFullPath(inputPath);

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandPath_WithInvalidPlaceholder()
    {
        var inputPath = "{InvalidPlaceholder}/AdorableMeow.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedPath = Path.GetFullPath(inputPath);

        Assert.Equal(expectedPath, expandedPath);
    }
}
