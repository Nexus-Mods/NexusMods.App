using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.Paths;

namespace NexusMods.Games.BethesdaGameStudios.Capabilities;

/// <summary>
/// Capability to support installing simple Data and GameRoot level mods for Bethesda games.
/// </summary>
public static class BethesdaInstallFolderTargets
{
    static readonly RelativePath DataFolder = new("data");

    // TODO: make this only contain values common for all bethesda games, let games add their own values
    // find good way to do that
    static readonly InstallFolderTarget DataInstallFolderTarget = new()
    {
        DestinationGamePath = new GamePath(GameFolderType.Game, DataFolder),

        KnownSourceFolderNames = new[] { "data" },

        KnownValidSubfolders = new[]
        {
            "fonts",
            "interface",
            "menus",
            "meshes",
            "music",
            "scripts",
            "shaders",
            "sound",
            "strings",
            "textures",
            "trees",
            "video",
            "facegen",
            "materials",
            "skse",
            "obse",
            "mwse",
            "nvse",
            "fose",
            "f4se",
            "distantlod",
            "asi",
            "SkyProc Patchers",
            "Tools",
            "MCM",
            "icons",
            "bookart",
            "distantland",
            "mits",
            "splash",
            "dllplugins",
            "CalienteTools",
            "NetScriptFramework",
            "shadersfx"
        },

        KnownValidFileExtensions = new[]
        {
            new Extension(".esp"),
            new Extension(".esm"),
            new Extension(".esl"),
            new Extension(".bsa"),
            new Extension(".ba2"),
            new Extension(".modgroups"),
        }
    };

    static readonly InstallFolderTarget GameRootInstallFolderTarget = new()
    {
        DestinationGamePath = new GamePath(GameFolderType.Game, RelativePath.Empty),

        KnownSourceFolderNames = new[]
        {
            "Mopy",
            "xLODGen",
            "DynDOLOD",
            "BethINI Standalone",
            "WrapperVersion"
        },

        KnownValidSubfolders = new[]
        {
            "data"
        },

        SubPathsToDiscard = new[]
        {
            new RelativePath("src")
        },

        SubTargets = new[]
        {
            DataInstallFolderTarget
        }
    };

    private static readonly InstallFolderTarget[] Targets = { GameRootInstallFolderTarget };

    /// <inheritdoc />
    public static IEnumerable<InstallFolderTarget> InstallFolderTargets()
    {
        return Targets;
    }
}
