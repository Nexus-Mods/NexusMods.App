# Stardew Valley

## Stores and Ids

- [Steam](https://store.steampowered.com/app/413150/Stardew_Valley/): `413150`
- [GOG](https://www.gog.com/game/stardew_valley): `1453375253`
- [Xbox Game Pass](https://www.xbox.com/en-US/games/store/p/9MWR1NC6VQ6L): `ConcernedApe.StardewValleyPC`

## Engine and Mod Support

Stardew Valley uses .NET 6 (.NET 5 for versions before 1.6) and [MonoGame](https://github.com/MonoGame/MonoGame), which is an open-source implementation of the discontinued [XNA Framework](https://en.wikipedia.org/wiki/Microsoft_XNA). MonoGame, and Stardew Valley to that extend, is fully cross-platform and runs natively on desktop (Windows/Linux/macOS), on the phone (Android/iOS) and on the console (PS4/PS5/Xbox using UWP and XDK/Nintendo Switch).

Note: Stardew Valley has a `compatability` branch for 32-bit systems, which uses XNA and isn't moddable.

Modding is done via [Stardew Modding API](https://smapi.io/), or _SMAPI_ for short. SMAPI is an [open-source](https://github.com/Pathoschild/SMAPI) modding framework and is supported on [Windows](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows), [Linux](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux) ([Steam Deck](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac)), [macOS](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac) and [Android](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android) (experimental at best).

## Game Files

- Windows: `Stardew Valley.exe`
- Linux and macOS: `StardewValley` (shell script that launches `Stardew Valley`)

The shell script only checks if the current macOS version is supported and launches the game.

Saves are located inside the AppData folder (`%AppData%` on Windows, `~/.config` on Linux) under `StardewValley/Saves`.

Additional, SMAPI puts global data files inside the AppData folder under `StardewValley/.smapi/mod-data/{modId}/{key}.json` ([Source](https://github.com/Pathoschild/SMAPI/blob/8d600e226960a81636137d9bf286c69ab39066ed/src/SMAPI/Framework/ModHelpers/DataHelper.cs#L163-L169)).

## Mod Formats

### C# Mods

Stardew Valley and SMAPI are written in C# with .NET 5. SMAPI exposes [APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs) and decompiled game code for modders to create mods using [C#](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started). All SMAPI mods require a [`manifest.json`](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest) file that is used by SMAPI to identify and load the mod and perform update checks:

```json
{
  "Name": "Your Project Name",
  "Author": "your name",
  "Version": "1.0.0",
  "Description": "One or two sentences about the mod.",
  "UniqueID": "YourName.YourProjectName",
  "EntryDll": "YourDllFileName.dll",
  "UpdateKeys": ["Nexus:1"],
  "MinimumApiVersion": "3.14.0",
  "Dependencies": [
    {
      "UniqueID": "SMAPI.ConsoleCommands",
      "MinimumVersion": "3.8.0",
      "IsRequired": false
    }
  ]
}
```

### Content Packs

C# mods are the most common Stardew Valley mods, however mods that don't required code and just change assets can be [Content Packs](https://stardewvalleywiki.com/Modding:Content_packs) instead. These mods are usually a collection of JSON and image files loaded by a specific [Content Pack Framework](https://stardewvalleywiki.com/Modding:Content_pack_frameworks), which itself is just a SMAPI C# mod. These mods also come with a [`manifest.json`](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest) file, but instead of specifying the `EntryDll`, they have to set the used content pack framework:

```json
{
  "Name": "Your Project Name",
  "Author": "your name",
  "Version": "1.0.0",
  "Description": "One or two sentences about the mod.",
  "UniqueID": "YourName.YourProjectName",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher",
    "MinimumVersion": "1.0.0"
  }
}
```

## Deployment

### SMAPI Mods

Modding is **exclusively** handled via SMAPI. Every mod, be it a C# mod or a Content Pack, goes inside the `Mods` folder in the game directory and must come with a `manifest.json` file. The manifest can be used to check for missing dependencies. Each mod has it's own sub-directory:

```text
Mods
├─ Mod A
│  ├─ manifest.json
├─ Mod B
│  ├─ manifest.json
```

Mods can also be grouped together:

```text
Mods
├─ Gameplay
│  ├─ Mod A
│  ├─ Mod B
├─ Graphics
│  ├─ Mod C
```

This grouping is completely optional and has no functional difference to a flat directory structure. SMAPI supports this nesting to help manual modding. Additionally, mods like [SVE](https://www.nexusmods.com/stardewvalley/mods/3753) which use multiple content pack frameworks, come with grouped mods for easier management.

If a mod doesn't have any special uninstall instructions (likely game related), the mod folder can just be deleted. A folder can also be _disabled_ by adding a dot in front of the folder name: `.Mod A`. Folders starting with a dot are ignored by SMAPI.

#### Config Files

If a SMAPI mod has settings can be tweaked by the user, then SMAPI will generate a `config.json` file inside the mod folder. Generation only happens after running the game. See [Pathoschild/SMAPI#928](https://github.com/Pathoschild/SMAPI/issues/928) for an issue to generate a `config.schema.json` file as well.

### SMAPI itself

SMAPI itself comes with a custom [installer](https://github.com/Pathoschild/SMAPI/tree/develop/src/SMAPI.Installer). The [downloaded archive](https://www.nexusmods.com/stardewvalley/mods/2400?tab=files) contains the following important files:

```text
SMAPI installer
├─ internal
│  ├─ windows
│  │  ├─ install.dat
│  ├─ macOS
│  │  ├─ install.dat
│  ├─ linux
│  │  ├─ install.dat
├─ install on macOS.command
├─ install on Linux.sh
├─ install on Windows.bat
```

Each supported platform has a shell script that launches the installer inside `internal/{os}`. The installer can update, install and uninstall SMAPI, but for our use-case, we just need the installation part:

The `install.dat` file is a ZIP archive containing all files that need to be extracted to the game folder. `install.dat` contains the following files:

```text
install.dat
├─ Mods
│  ├─ ConsoleCommands/
│  ├─ ErrorHandler/
│  ├─ SaveBackup/
├─ smapi-internal/
├─ steam_appid.txt
```

Windows also has `StardewModdingAPI.exe` and Linux and macOS have `unix-launcher.sh` and `StardewModdingAPI` instead.

Regardless of platform, similarly to Bethesda Script Extenders, SMAPI comes with it's own loader executable called `StardewModdingAPI.exe` (Windows) or `StardewModdingAPI` (Unix). The installer also copies `Stardew Valley.deps.json` from the game folder to `StardewModdingAPI.deps.json`, so that native DLLs resolve properly. The following steps are dependent on platform and store front:

- Linux and macOS: `unix-launcher.sh` from `install.dat` replaces the original game launcher script `StardewValley`
- Windows (Steam and GOG): `StardewModdingAPI.exe` can be launched directly. Alternatively, [Steam](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Steam) and [GOG Galaxy](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#GOG_Galaxy) can be configured to launch `StardewModdingAPI.exe` instead of the original game executable `Stardew Valley.exe`. This is only needed if you want achievements and playtime tracking to work.
- Windows ([Xbox Game Pass](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Xbox_app)): the original game executable `Stardew Valley.exe` has to be replaced with `StardewModdingAPI.exe` for mods to work.

## Mod Conflicts

### File Conflicts

Before SMAPI Content Packs and the Content Patcher were a thing, assets had to be changed by replacing the raw XNB files in the `Content` directory. Most XNB mods have been [migrated](https://forums.stardewvalley.net/threads/migrating-xnb-mods-to-content-patcher-packs.564/) to the Content Patcher format, but some older mods didn't receive an "official" update. The wiki contains a [list of unofficial updates](https://stardewvalleywiki.com/Modding:Using_XNB_mods#Alternatives_using_Content_Patcher).

Two mods that replace the same XNB file are in conflict with each other and the order in which they are deployed determines the winner.

### C# Mods

C# Mods can target an incompatible version of SMAPI or of the game itself. Conflicts between SMAPI mods are logic/functionality based.

SMAPI Mods can define a [minimum SMAPI version](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Minimum_SMAPI_version). Since SMAPI versions are tied to game versions, you indirectly specify what game versions the mod supports.

### Content Packs

SMAPI Content Packs on their own don't do anything. They require a [Content Pack Framework](https://stardewvalleywiki.com/Modding:Content_pack_frameworks) which is a SMAPI C# Mod that updates the assets based on rules defined in the Content Pack.

Generally speaking, Content Packs are applied by the Content Pack Framework in the order that SMAPI loads them. This order is defined by the dependencies in the `manifest.json` file.

As an example, let's say we have three mods: `Pathoschild.ContentPatcherr`, `Foo` and `Bar`.

```json
{
  "UniqueID": "Pathoschild.ContentPatcher"
}
```

```json
{
  "UniqueID": "Foo",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
  }
}
```

```json
{
  "UniqueID": "Bar",
  "ContentPackFor": {
    "UniqueID": "Pathoschild.ContentPatcher"
  },
  "Dependencies": [
    {
      "UniqueID": "Foo"
    }
  ]
}
```

SMAPI will first load the C# mod `Pathoschild.ContentPatcher`. The Content Patcher mod will get a list of owned Content Packs from SMAPI. This list is already sorted for dependencies. In the example above, `Foo` will be applied _before_ `Bar` because `Bar` has a dependency on `Foo`. If both `Foo` and `Bar` edit the same asset, the edits of `Bar` will be applied last.

Detecting if two Content Packs are in conflict with one another depends on the Content Pack Framework. Each framework has its own way of modifying existing, or adding new assets.

## Community

- [Stardew Valley Modding Wiki](https://stardewvalleywiki.com/Modding:Index)
- [Stardew Valley Discord](https://discord.gg/stardewvalley)
- [r/SMAPI](https://www.reddit.com/r/SMAPI)
- [Stardew Valley Forums](https://forums.stardewvalley.net/)

## Additional Notes

SMAPI provides the following additional web services:

- [JSON validator](https://smapi.io/json) for `manifest.json` files ([JSON Schema](https://smapi.io/schemas/manifest.json))
- [Log Parser](https://smapi.io/log)
- [Mod compatibility spreadsheet](https://smapi.io/mods)
- [Web API](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/web.md#web-api) with a `/mods` [endpoint](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/web.md#mods-endpoint) to query for metadata. This can be used for update checking across mod sources.
