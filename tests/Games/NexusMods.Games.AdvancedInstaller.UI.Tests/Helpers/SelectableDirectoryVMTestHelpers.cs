using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class SelectableDirectoryVMTestHelpers
{
    internal static InMemoryFileSystem CreateInMemoryFs(AbsolutePath basePath)
    {
        var fs = new InMemoryFileSystem();
        fs.AddPaths(basePath, GetPaths());
        return fs;
    }

    private static RelativePath[] GetPaths() => ResultsVMTestHelpers.GetPaths();

    private static RelativePath[] GetSavePaths()
    {
        return new[]
        {
            new RelativePath("config.cfg"),
            new RelativePath("saves/save001.sav"),
            new RelativePath("saves/save002.sav"),
            new RelativePath("saves/save003.sav"),
            new RelativePath("saves/save004.sav")
        };
    }

    /// <summary>
    /// Asserts that a child node exists with the given name, and returns said node.
    /// </summary>
    internal static TreeEntryViewModel AssertChildNode(TreeEntryViewModel parentNode, string nodeName)
    {
        var node = parentNode.Children.First(x => x.DisplayName == nodeName);
        node.Should().NotBeNull($"because {nodeName} should exist");
        return (node as TreeEntryViewModel)!;
    }

    internal static void AddSavePaths(this InMemoryFileSystem fs, AbsolutePath basePath) =>
        fs.AddPaths(basePath, GetSavePaths());

    private static void AddPaths(this InMemoryFileSystem fs, AbsolutePath basePath, RelativePath[] paths)
    {
        foreach (var path in paths)
            fs.AddEmptyFile(fs.FromUnsanitizedFullPath(basePath + "/" + path));
    }
}
