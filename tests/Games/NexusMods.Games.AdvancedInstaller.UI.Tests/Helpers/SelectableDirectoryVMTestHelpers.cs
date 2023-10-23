using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class SelectableDirectoryVMTestHelpers
{
    internal static InMemoryFileSystem CreateInMemoryFs(AbsolutePath basePath)
    {
        var fs = new InMemoryFileSystem();
        foreach (var path in GetPaths())
            fs.AddEmptyFile(fs.FromUnsanitizedFullPath(basePath + "/" + path));

        return fs;
    }

    private static RelativePath[] GetPaths() => ResultsVMTestHelpers.GetPaths();

    /// <summary>
    /// Asserts that a child node exists with the given name, and returns said node.
    /// </summary>
    internal static TreeEntryViewModel AssertChildNode(TreeEntryViewModel parentNode, string nodeName)
    {
        var node = parentNode.Children.First(x => x.DisplayName == nodeName);
        node.Should().NotBeNull($"because {nodeName} should exist");
        return (node as TreeEntryViewModel)!;
    }
}
