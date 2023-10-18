using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class SelectableDirectoryNodeTestHelpers
{
    internal static InMemoryFileSystem CreateInMemoryFs()
    {
        var fs = new InMemoryFileSystem();
        foreach (var path in GetPaths())
            fs.AddEmptyFile(fs.FromUnsanitizedFullPath("/" + path));

        return fs;
    }

    private static RelativePath[] GetPaths() => ResultsNodeTestHelpers.GetPaths();


    /// <summary>
    /// Asserts that a child node exists with the given name, and returns said node.
    /// </summary>
    internal static SelectableDirectoryNode AssertChildNode(SelectableDirectoryNode parentNode, string nodeName)
    {
        var node = parentNode.Children.First(x => x.Node.AsT1.DirectoryName == nodeName);
        node.Should().NotBeNull($"because {nodeName} should exist");
        return (node.Node.AsT1 as SelectableDirectoryNode)!;
    }
}
