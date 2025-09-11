using NexusMods.Abstractions.GameLocators;

namespace NexusMods.Games.CreationEngine;

public static class KnownPaths
{
    public static readonly GamePath Game = new GamePath(LocationId.Game, "");
    public static readonly GamePath Data = new GamePath(LocationId.Game, "Data");
    public static readonly GamePath SKSE64Loader = new GamePath(LocationId.Game, "skse64_loader.exe");
    public static readonly GamePath F4SELoader = new GamePath(LocationId.Game, "f4se_loader.exe");
    
    /// <summary>
    /// Common top level folders for use in the StopPatternInstaller.
    /// </summary>
    public static readonly string[] CommonTopLevelFolders =
    [
        "asi",
        "calientetools",
        "distantlod",
        "facegen",
        "fonts",
        "interface",
        "lodsettings",
        "lsdata",
        "menus",
        "meshes",
        "music",
        "scripts",
        "shaders",
        "sound",
        "strings",
        "textures",
        "tools",
        "trees",
        "video",
    ];
}
