using Vogen;

namespace NexusMods.DataModel.Games { }

namespace NexusMods.Paths
{
    /// <summary>
    ///     The base folder for the GamePath, more values can easily be added here as needed
    /// </summary>
    [ValueObject<int>]
    [Instance("Game", 0, "The path for the game installation.")]
    [Instance("Saves", 1, "Path used to store the save data of a game.")]
    [Instance("Preferences", 2, "Path used to store player settings/preferences.")]
    [Instance("AppData", 3, "Path for game files located under LocalAppdata or equivalent")]
    [Instance("AppDataRoaming", 4, "Path for game files located under Appdata/Roaming or equivalent")]
    [Instance("Documents", 5,
        "Path for game files located under Documents or equivalent (e.g. Documents/My Games/GameName)")]
    public readonly partial struct GameFolderType {}
}
