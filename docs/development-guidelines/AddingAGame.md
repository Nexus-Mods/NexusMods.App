# Adding a Game

!!! info "Describes how to add a game to the Nexus App."

??? "Example Game (Click to Expand)"

    ```csharp
    /// <summary>
    /// Adds file extraction related services to the provided DI container.
    /// </summary>
    /// <param name="coll">Service collection to register.</param>
    /// <param name="settings">Settings for the extractor.</param>
    /// <returns>Service collection passed as parameter.</returns>
    public static IServiceCollection AddFileExtractors(this IServiceCollection coll, IFileExtractorSettings? settings = null)
    {
        if (settings == null)
            coll.AddSingleton<IFileExtractorSettings, FileExtractorSettings>();
        else
            coll.AddSingleton(settings);

        coll.AddSingleton<IFileExtractor, FileExtractor>();
        coll.AddSingleton<IExtractor, SevenZipExtractor>();
        coll.TryAddSingleton<TemporaryFileManager, TemporaryFileManagerEx>();
        return coll;
    }
    ```

### `IGame` Interface

The primary entrance point for adding a game to the app is the IGame interface. If the game being added is part
of a family of games based on the same engine or developed by the same developer, consider adding the code to an existing
library. Otherwise, create a new .dll project in the Games folder.

Although the IGame interface is the most abstract interface for adding games, there are a lot of common
facilities in `AGame` that maybe useful in your implementation. So start by sublcassing `AGame` and adding interfaces
for ISteamGame, IGogGame, etc. as needed. Filling out all these abstract methods for these classes/interfaces
will get you most of the way to supporting the game.

A few things to keep in mind:
* The Domain property should match the NexusMods domain for the game. For example, the domain for Skyrim Special Edition is `skyrimspecialedition`.
* The GamePaths property should be populated with the paths to the game's executable and any other files in other folders that should be monitored for by the app. This includes saved games, profiles, inis, etc.
* Be sure that the GamePaths are mapped to the correct locations for a given installation of a game GOG may place saved games in a different location than Steam, for example.
* The PrimaryFile is the game's primary executable.

Next add your copy of IGame to the DI system by registering it in the `Services.cs` file in your project. If you have added
a project from scratch you will also need to reference it in `NexusMods.App.csproj` and add a call to `Services.AddYourGame()` to the top level DI registration system.

### `IFileAnalyzer` Interface
This interface can be used to generate new file metadata to be saved for when the app performs analysis of a mod file. When an archive
is set to be installed by the app, the app first analyzes the archive, running the contents (and the archive itself) through all `IFileAnalyzer` interfaces. If you have file specific
metadata to extract, create an implemenation of this interface and register it with the DI system. For example, the Skyrim game support adds an analyzer
for plugin files. This analysis data includes the plugin's masters so that the app can properly sort the plugins later on.
