using FluentAssertions;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.ModContent;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class AdvancedInstallerTestHelpers
{
    internal static InMemoryFileSystem CreateInMemoryFs(AbsolutePath basePath) => new();

    internal static FileTreeNode<RelativePath, ModSourceFileEntry> CreateTestTreeMSFE()
    {
        var fileEntries = new Dictionary<RelativePath, ModSourceFileEntry>
        {
            { new RelativePath("BWS.bsa"), null! },
            { new RelativePath("BWS - Textures.bsa"), null! },
            { new RelativePath("Readme-BWS.txt"), null! },
            { new RelativePath("Textures/greenBlade.dds"), null! },
            { new RelativePath("Textures/greenBlade_n.dds"), null! },
            { new RelativePath("Textures/greenHilt.dds"), null! },
            { new RelativePath("Textures/Armors/greenArmor.dds"), null! },
            { new RelativePath("Textures/Armors/greenBlade.dds"), null! },
            { new RelativePath("Textures/Armors/greenHilt.dds"), null! },
            { new RelativePath("Meshes/greenBlade.nif"), null! }
        };

        return FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(fileEntries);
    }

    internal static FileTreeNode<RelativePath, ModSourceFileEntry> CreateTestFileTree()
    {
        var mockModSourceFileEntry = new ModSourceFileEntry
        {
            StreamFactory = null!,
            Hash = Hash.FromLong(0),
            Size = Size.FromLong(0),
        };

        var fileEntries = new Dictionary<RelativePath, ModSourceFileEntry>
        {
            { new RelativePath("Blue Version/Data/file1.txt"), mockModSourceFileEntry },
            { new RelativePath("Blue Version/Data/file2.txt"), mockModSourceFileEntry },
            { new RelativePath("Blue Version/Data/pluginA.esp"), mockModSourceFileEntry },
            { new RelativePath("Blue Version/Data/PluginB.esp"), mockModSourceFileEntry },
            { new RelativePath("Blue Version/Data/Textures/textureA.dds"), mockModSourceFileEntry },
            { new RelativePath("Blue Version/Data/Textures/textureB.dds"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/file1.txt"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/file3.txt"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/pluginA.esp"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/PluginC.esp"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/Textures/textureA.dds"), mockModSourceFileEntry },
            { new RelativePath("Green Version/data/Textures/textureC.dds"), mockModSourceFileEntry },
        };

        return FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(fileEntries);
    }
}
