using FluentAssertions;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class SelectableDirectoryVMTestHelpers
{
    internal static ITreeEntryViewModel GetChild(this ITreeEntryViewModel vm, string childName)
    {
        return vm.Children.First(x => x.DisplayName == childName);
    }

    internal static InMemoryFileSystem CreateInMemoryFs(AbsolutePath basePath) => new();

    internal static RelativePath[] GetGameFolderPaths()
    {
        return new[]
        {
            new RelativePath("SkyrimSE.exe"),
            new RelativePath("SkyrimSELauncher.exe"),
            new RelativePath("steam_api64.dll"),
            new RelativePath("Data/Skyrim.esm"),
            new RelativePath("Data/Skyrim - Textures0.bsa"),
            new RelativePath("Data/Skyrim - Textures1.bsa"),
            new RelativePath("Data/Textures/redBlade.dds"),
            new RelativePath("Data/Textures/redBlade_n.dds"),
            new RelativePath("Data/Textures/redHilt.dds"),
            new RelativePath("Data/Textures/Armors/redArmor.dds"),
            new RelativePath("Data/Textures/Armors/redBlade.dds"),
            new RelativePath("Data/Textures/Armors/redHilt.dds"),
            new RelativePath("Data/Meshes/redBlade.nif")
        };
    }

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

    internal static void AddPaths(this InMemoryFileSystem fs, AbsolutePath basePath, RelativePath[] paths)
    {
        foreach (var path in paths)
            fs.AddEmptyFile(fs.FromUnsanitizedFullPath(basePath + "/" + path));
    }
}
