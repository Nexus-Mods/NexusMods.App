using NexusMods.DataModel.Abstractions.DTOs;
using NexusMods.DataModel.Trees;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests.Helpers;

internal static class AdvancedInstallerTestHelpers
{
    internal static InMemoryFileSystem CreateInMemoryFs() => new();

    internal static KeyedBox<RelativePath, ModFileTree> CreateTestFileTree()
    {
        var fileEntries = new DownloadContentEntry[]
        {
            new() { Hash = Hash.From(1), Size = Size.From(1), Path = "Blue Version/Data/file1.txt" },
            new() { Hash = Hash.From(2), Size = Size.From(2), Path = "Blue Version/Data/file2.txt" },
            new() { Hash = Hash.From(3), Size = Size.From(3), Path = "Blue Version/Data/pluginA.esp" },
            new() { Hash = Hash.From(4), Size = Size.From(4), Path = "Blue Version/Data/PluginB.esp" },
            new() { Hash = Hash.From(5), Size = Size.From(5), Path = "Blue Version/Data/Textures/textureA.dds" },
            new() { Hash = Hash.From(6), Size = Size.From(6), Path = "Blue Version/Data/Textures/textureB.dds" },
            new() { Hash = Hash.From(7), Size = Size.From(7), Path = "Green Version/data/file1.txt" },
            new() { Hash = Hash.From(8), Size = Size.From(8), Path = "Green Version/data/file3.txt" },
            new() { Hash = Hash.From(9), Size = Size.From(9), Path = "Green Version/data/pluginA.esp" },
            new() { Hash = Hash.From(10), Size = Size.From(10), Path = "Green Version/data/PluginC.esp" },
            new() { Hash = Hash.From(11), Size = Size.From(11), Path = "Green Version/data/Textures/textureA.dds" },
            new() { Hash = Hash.From(12), Size = Size.From(12), Path = "Green Version/data/Textures/textureC.dds" }
        };

        return ModFileTree.Create(fileEntries);
    }
}
