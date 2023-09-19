using Vogen;

namespace NexusMods.DataModel.Games { }

namespace NexusMods.Paths
{
    /// <summary>
    ///     The base folder for the GamePath, more values can easily be added here as needed
    /// </summary>
    [ValueObject<string>]
    [Instance("Game", "Game", "The path for the game installation.")]
    [Instance("Saves", "Saves", "Path used to store the save data of a game.")]
    [Instance("Preferences", "Preferences", "Path used to store player settings/preferences.")]
    [Instance("AppData", "AppData", "Path for game files located under LocalAppdata or equivalent")]
    [Instance("AppDataRoaming", "AppDataRoaming", "Path for game files located under Appdata/Roaming or equivalent")]
    [Instance("Documents", "Documents",
        "Path for game files located under Documents or equivalent (e.g. Documents/My Games/GameName)")]
    public readonly partial struct GameFolderType { }
}
