# Stardew Valley

## Stores and Ids

- [Steam](https://store.steampowered.com/app/413150/Stardew_Valley/): `413150`
- [GOG](https://www.gog.com/game/stardew_valley): `1453375253`
- [Xbox Game Pass](https://www.xbox.com/en-US/games/store/p/9MWR1NC6VQ6L): `UNKNOWN`

## Engine and Mod Support

Stardew Valley uses .NET 5 and [MonoGame](https://github.com/MonoGame/MonoGame), which is an open-source implementation of the discontinued [XNA Framework](https://en.wikipedia.org/wiki/Microsoft_XNA). MonoGame, and Stardew Valley to that extend, is fully cross-platform and runs natively on desktop (Windows/Linux/macOS), on the phone (Android/iOS) and on the console (PS4/PS5/Xbox using UWP and XDK/Nintendo Switch).

Modding is done via [Stardew Modding API](https://smapi.io/), or _SMAPI_ for short. SMAPI is an [open-source](https://github.com/Pathoschild/SMAPI) modding framework and is supported on [Windows](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows), [Linux](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Linux) ([Steam Deck](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac)), [macOS](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Mac) and [Android](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Android) (experimental at best).

## Game Files

- Windows: `Stardew Valley.exe`
- Linux and macOS: `StardewValley` (shell script that launches `Stardew Valley`)

The shell script only checks if the current macOS version is supported and launches the game.

Saves are located inside the AppData folder (`%AppData%` on Windows, `~/.config` on Linux) under `StardewValley/Saves`. SMAPI puts the saves inside `StardewValley/.smapi` instead (this has changed in [`3.2`](https://github.com/Pathoschild/SMAPI/blob/9763bc7484e29cbc9e7f37c61121d794e6720e75/src/SMAPI.Installer/InteractiveInstaller.cs#L358), the previous location was `StardewValley/Saves/.smapi`).

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

This grouping is completely optional and has no functional difference to a flat directory structure. SMAPI supports this nesting to help manual modding.

If a mod doesn't have any special uninstall instructions (likely game related), the mod folder can just be deleted. A folder can also be _disabled_ by adding a dot in front of the folder name: `.Mod A`. Folders starting with a dot are ignored by SMAPI.

#### SMAPI itself

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

1) `install.dat` is a ZIP archive and all files inside will be extracted to the game folder
2) on Linux and macOS: the original game launcher gets replaced with `unix-launcher.sh`, which was extracted from `install.dat`. The installer also runs `chmod` to mark the file as executable.
3) `Stardew Valley.deps.json` from the game folder gets copied to `StardewModdingAPI.deps.json` (required to resolve native DLLs)
4) Windows only: SMAPI comes with it's own executable `StardewModdingAPI.exe` which must be used to launch the game. The [wiki](https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows#Configure_your_game_client) has instructions to configure the game client correctly.

The `install.dat` ZIP archive contains the following files:

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

## Additional Notes

SMAPI provides the following additional web services:

- [JSON validator](https://smapi.io/json) for `manifest.json` files ([JSON Schema](https://smapi.io/schemas/manifest.json))
- [Log Parser](https://smapi.io/log)
- [Mod compatibility spreadsheet](https://smapi.io/mods)
- [Web API](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/web.md#web-api) with a `/mods` [endpoint](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/web.md#mods-endpoint) to query for metadata. This can be used for update checking across mod sources.
