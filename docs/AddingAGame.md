# Adding a Game

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


## Mod Installation
Mod installation in the app happens in several phases
1. The user selects a mod to install
2. The app extracts the archive
3. Each file in the archive is analyzed by the `IFileAnalyzer` implementations
4. The app feeds the files (and metadata from analysis) to the `IModInstaller` implementation with the winning priority.
5. The `IModInstaller` returns a list of `AModFile` objects that determine how to install the mod into the various `GamePath` folders.

All analysis data is cached in the internal KV store so that the app doesn't have to re-analyze files every time it installs a mod.

### `IModInstaller` Interface
Now that you have your game registered with the DI system you may want to install a mod. The `IModInstaller` interface is used
as a way to register handlers for specific types of mods. The logic here is game specific, some games may support automatic installation via
detecting the file types and names. For example, Cyberpunk may know that `.archive` files are mods and can be installed by copying them to the mods folder. Or Skyrim installers
may know that `plugins/*.dll` files are skse plugins and should be copied to the skse plugins folder.

### `IFileAnalyzer` Interface
This interface can be used to generate new file metadata to be saved for when the app performs analysis of a mod file. When an archive
is set to be installed by the app, the app first analyzes the archive, running the contents (and the archive itself) through all `IFileAnalyzer` interfaces. If you have file specific
metadata to extract, create an implemenation of this interface and register it with the DI system. For example, the Skyrim game support adds an analyzer
for plugin files. This analysis data includes the plugin's masters so that the app can properly sort the plugins later on.
