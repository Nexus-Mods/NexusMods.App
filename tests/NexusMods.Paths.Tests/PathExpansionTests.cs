using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class PathExpansionTests
{
    [Fact]
    public void CanExpandEntryFolder()
    {
        var inputPath = "{EntryFolder}/FluffyKitten.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.Combine(KnownFolders.EntryFolder.GetFullPath(), "FluffyKitten.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandCurrentDirectory()
    {
        var inputPath = "{CurrentDirectory}/AdorableWhiskers.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.Combine(KnownFolders.CurrentDirectory.GetFullPath(), "AdorableWhiskers.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandHomeFolder()
    {
        var inputPath = "{HomeFolder}/CuddlyFurball.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.Combine(KnownFolders.HomeFolder.GetFullPath(), "CuddlyFurball.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandMyGames()
    {
        var inputPath = "{MyGames}/CuriousWhiskers.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.Combine(KnownFolders.MyGames.GetFullPath(), "CuriousWhiskers.png");

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandPath_WithNoPlaceholder()
    {
        var inputPath = "AdorableMeow.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.GetFullPath(inputPath);

        Assert.Equal(expectedPath, expandedPath);
    }

    [Fact]
    public void CanExpandPath_WithInvalidPlaceholder()
    {
        var inputPath = "{InvalidPlaceholder}/AdorableMeow.png";
        var expandedPath = KnownFolders.ExpandPath(inputPath);
        var expectedPath = Path.GetFullPath(inputPath);

        Assert.Equal(expectedPath, expandedPath);
    }
}
