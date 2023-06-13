using FluentAssertions;
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

    [Theory]
    [InlineData("{EntryFolder}/FluffyKitten.png", KnownPath.EntryDirectory, "FluffyKitten.png")]
    [InlineData("{CurrentDirectory}/AdorableWhiskers.png", KnownPath.CurrentDirectory, "AdorableWhiskers.png")]
    [InlineData("{HomeFolder}/CuddlyFurball.png", KnownPath.HomeDirectory, "CuddlyFurball.png")]
    [InlineData("{MyGames}/CuriousWhiskers.png", KnownPath.MyGamesDirectory, "CuriousWhiskers.png")]
    public void CanExpandKnownFolder(string inputPath, KnownPath knownPath, string expectedPath)
    {
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        var expectedFullPath = _fileSystem.FromUnsanitizedFullPath(Path.Combine(_fileSystem.GetKnownPath(knownPath).GetFullPath(), expectedPath));

        expandedPath.Should().Be(expectedFullPath);
    }

    [Fact]
    public void CanExpandPath_WithNoPlaceholder()
    {
        const string inputPath = "AdorableMeow.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        expandedPath.GetFullPath().Should().Be(inputPath);
    }

    [Fact]
    public void CanExpandPath_WithInvalidPlaceholder()
    {
        const string inputPath = "{InvalidPlaceholder}/AdorableMeow.png";
        var expandedPath = _fileSystem.ExpandKnownFoldersPath(inputPath);
        expandedPath.GetFullPath().Should().Be(inputPath);
    }
}
