# Adding a Game

!!! info "Describes how to add a game to the Nexus Mods App."

To have a game be automatically recognised by the Nexus Mods App, create a class that inherits from `AGame`.

??? "Example Game (Click to Expand)"

    ```csharp
    public class Sifu : AGame, ISteamGame, IEpicGame
    {
        // Name of your game.
        public override string Name => "Sifu";

        // Game 'Domain' on Nexus, i.e. https://www.nexusmods.com/sifu is 'sifu'
        public override GameDomain Domain => GameDomain.From("sifu");

        // Path to EXE
        public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "Sifu.exe");

        // [ISteamGame] Steam ID
        // Extract this from SteamDB e.g. https://steamdb.info/app/2138710/ or your `appmanifest_` files in `SteamApps`.
        public IEnumerable<uint> SteamIds => new[] { 2138710u };

        // [IEpicGame] 'CatalogItemId' from JSON,
        // see https://github.com/erri120/GameFinder/wiki/Epic-Games-Store
        public IEnumerable<string> EpicCatalogItemId => new[] { "c80a76de890145edbe0d41679dbccc66" };

        public Sifu(IServiceProvider serviceProvider) : base(serviceProvider) { }

        // Registers supported 'Mod Installers'
        protected override IEnumerable<IModInstaller> MakeInstallers(IServiceProvider provider) => new[] { new SifuModInstaller(provider) };

        // Boilerplate for games that don't do anything special. //
        protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
            GameLocatorResult installation)
        {
            return new Dictionary<LocationId, AbsolutePath>()
            {
                { LocationId.Game, installation.Path },
            };
        }

        public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
            => ModInstallDestinationHelpers.GetCommonLocations(locations);
    }
    ```

## Making the App Auto-Detect Your Game

To add automatic detection for a given game, make your `AGame` implement the following interfaces (where applicable).

| Store      | Interface Name | Where to manually find store specific 'App ID'(s)                                                                                              |
|------------|----------------|------------------------------------------------------------------------------------------------------------------------------------------------|
| EA Desktop | IEADesktopGame | [It's complicated.][ea-gamefinder]                                                                                                             |
| Epic       | IEpicGame      | Extract `CatalogItemId` from JSON file in <br>`C:\Program Data\Epic\EpicGamesLauncher\Data\Manifests`.<br/> [Reference docs.][epic-gamefinder] |
| GOG Galaxy | IGogGame       | Go to `HKEY_LOCAL_MACHINE\Software\GOG.com\Games` <br/>in registry and grab from `gameID` field.  [Reference docs.][gog-gamefinder]            |
| Origin     | IOriginGame    | Get from `.mfst` file in `C:\Program Data\Origin\LocalContent`. Copy paste everything after `?id=`.                                            |
| Steam      | ISteamGame     | Extract 'AppId' from SteamDB ([Example][steamdb-example]).<br/> Or your `appmanifest_{AppId}.acf` files in `SteamApps` folder.                 |
| Xbox       | IXboxGame      | Find inside `appxmanifest.xml` in game folder. Extract from `Name` field under `Identity`.                                                     |

### Quickly Finding the 'App IDs'

!!! tip "Builds of the Nexus Mods App in `Debug` configuration will print all found games."

```
[DEBUG] Found Steam Game: 252950, Rocket League
[DEBUG] Found Steam Game: 389730, TEKKEN 7
[DEBUG] Found Steam Game: 71340, Sonic Generations
```

So if you have the game you want to add support for installed, you can find the needed 'id' in the log.

In that scenario, to implement `Rocket League`, you would inherit `ISteamGame` and add `252950` as the ID.

```csharp
public IEnumerable<uint> SteamIds => new[] { 252950u };
```

!!! tip "If you have a game that's not tied to a particular store, you'll probably need to extend the `AGameLocator` class to implement the logic to 'find' your game."

## Populating Game Paths

!!! tip "You need to tell the app where to look for files by overriding `GetLocations`."

Example:
```csharp
protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
{
    return new Dictionary<LocationId, AbsolutePath>
    {
        { LocationId.Game, installation.Path },
        { LocationId.AppData, fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("Skyrim") }
    };
}
```

Here we add the game folder and `AppData` folder to `Skyrim LE`.

When you are overwriting `GetLocations`, you should include all paths that the app needs to track,
i.e. `Saves`, `Configs`, `Game Folder` etc.

The function `GetPrimaryFile` is the game's main executable (not a launcher).

!!! warning "Sometimes games on different storefronts may have different locations. For example, a game installed via GOG might have a different save path than the Steam installation."

## Linking your Game with NexusMods

!!! tip "To link your game with a Nexus page, you need to set the 'Domain' field."

Each `AGame` instance has a field named `Domain`. This `Domain` corresponds to the URL used on Nexus Mods for the game.

So for example, if you have [https://www.nexusmods.com/sifu][sifu], you should set the value to `sifu`

```csharp
public override GameDomain Domain => GameDomain.From("sifu");
```

## Add your Game to the Dependency Injection Container

!!! info "For the App to recognise your game, you'll need to add it to the [Dependency Injection][dependency-injection] container."

More specifically, you will need to register your `IGame` in your project's `Services.cs` file (create it if it doesn't exist).

```csharp
public static IServiceCollection AddSifu(this IServiceCollection serviceCollection)
{
    serviceCollection.AddAllSingleton<IGame, Sifu>();
    serviceCollection.AddAllSingleton<IModInstaller, SifuModInstaller>();
    return serviceCollection;
}
```

After that, you may also need to add it to [AddApp][add-app] and call `services.AddYourGame()`.

## General Guidelines

### You Might want to Group Similar Games in One Project

!!! tip "If your code targets a specific common 'game engine' or middleware, you might want to put your logic there."

For example, the Nexus Mods App has `NexusMods.Games.BethesdaGameStudios`, which contains code for the `Creation` engine.

This code is used by games such as `Skyrim LE` and `Skyrim SE`, so these two games live in the
`NexusMods.Games.BethesdaGameStudios` project.

[add-app]: https://github.com/Nexus-Mods/NexusMods.App/blob/71ed7f186c6a5fe0dd0e45e2cf24c7a624c1bed4/src/NexusMods.App/Services.cs#L51
[dependency-injection]: ./DependencyInjection.md#how-does-it-know
[ea-gamefinder]: https://github.com/erri120/GameFinder/wiki/EA-Desktop
[epic-gamefinder]: https://github.com/erri120/GameFinder/wiki/Epic-Games-Store
[gog-gamefinder]: https://github.com/erri120/GameFinder/wiki/GOG-Galaxy
[sifu]: https://www.nexusmods.com/sifu
[steamdb-example]: https://steamdb.info/app/2138710/
