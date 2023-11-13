using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class BodyDesignViewModel : BodyViewModel
{
    public BodyDesignViewModel() : base(
        new DeploymentData(),
        "Design Mod Name",
        CreateDesignFileTree(),
        CreateDesignGameLocationsRegister(),
        null) { }

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

    internal static FileTreeNode<RelativePath, ModSourceFileEntry> CreateDesignFileTree()
    {
        var mockModSourceFileEntry = new ModSourceFileEntry
        {
            StreamFactory = null!,
            Hash = Hash.FromLong(0),
            Size = Size.FromLong(0),
        };

        var fileEntries = new Dictionary<RelativePath, ModSourceFileEntry>
        {
            { new RelativePath("BWS.bsa"), mockModSourceFileEntry },
            { new RelativePath("BWS - Textures.bsa"), mockModSourceFileEntry },
            { new RelativePath("Readme-BWS.txt"), mockModSourceFileEntry },
            { new RelativePath("Textures/greenBlade.dds"), mockModSourceFileEntry },
            { new RelativePath("Textures/greenBlade_n.dds"), mockModSourceFileEntry },
            { new RelativePath("Textures/greenHilt.dds"), mockModSourceFileEntry },
            { new RelativePath("Textures/Armors/greenArmor.dds"), mockModSourceFileEntry },
            { new RelativePath("Textures/Armors/greenBlade.dds"), mockModSourceFileEntry },
            { new RelativePath("Textures/Armors/greenHilt.dds"), mockModSourceFileEntry },
            { new RelativePath("Meshes/greenBlade.nif"), mockModSourceFileEntry }
        };

        return FileTreeNode<RelativePath, ModSourceFileEntry>.CreateTree(fileEntries);
    }
}
