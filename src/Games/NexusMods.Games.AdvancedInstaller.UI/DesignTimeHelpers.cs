using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;
using NexusMods.Paths.Trees;

namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
/// Collection of convenience methods to generate design-time mock data.
/// </summary>
internal static class DesignTimeHelpers
{
    /// <summary>
    /// Create a mock game locations register for design-time purposes.
    /// </summary>
    /// <returns></returns>
    internal static GameLocationsRegister CreateDesignGameLocationsRegister()
    {
        var fs = new InMemoryFileSystem();
        var locations = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, fs.FromUnsanitizedFullPath("C:/Games/Skyrim Special Edition") },
            { LocationId.AppData, fs.FromUnsanitizedFullPath("C:/Users/Me/AppData/Local/Skyrim Special Edition") },
            { LocationId.From("Data"), fs.FromUnsanitizedFullPath("C:/Games/Skyrim Special Edition/Data") },
        };

        var register = new GameLocationsRegister(locations);
        return register;
    }

    /// <summary>
    /// Create a mock file tree of mod archive contents for design-time purposes.
    /// </summary>
    /// <returns></returns>
    internal static KeyedBox<RelativePath, ModFileTree> CreateDesignFileTree()
    {
        return ModFileTree.Create(new ModFileTreeSource[]
        {
            new(0, 0, "BWS.bsa"),
            new(0, 0, "BWS - Textures.bsa"),
            new(0, 0, "Readme-BWS.txt"),
            new(0, 0, "Textures/greenBlade.dds"),
            new(0, 0, "Textures/greenBlade_n.dds"),
            new(0, 0, "Textures/greenHilt.dds"),
            new(0, 0, "Textures/Armors/greenArmor.dds"),
            new(0, 0, "Textures/Armors/greenBlade.dds"),
            new(0, 0, "Textures/Armors/greenHilt.dds"),
            new(0, 0, "Meshes/greenBlade.nif")
        });
    }
}
