# Context and Problem Statement

!!! note "This is a design document"

    This document focuses more on the technical steps needed as opposed to what
    the different 'options' are.

As users use the Nexus App, there may be a necessity to run certain modding tools
that are only compiled for the Windows Operating System.

## Severity

!!! danger "This is a high priority issue."

This is fairly important, as running external tools is needed only for
running externally downloaded modding tools. We do in fact
***sometimes need to run tools as part of the apply step***.

For examples:

- FNIS (Skyrim)
- RedMod (Cyberpunk 2077)

etc.

## Why We Need to Support Windows Tools

!!! info "Which tools do we need to run with a compatibility layer?"

Supporting Windows tools is crucial for our mod manager due to two main
categories of software:

### Windows-Specific Development Frameworks

!!! info "Some modding tools are built using Windows-only like WPF for the UI."

Porting these tools can sometimes be expensive/unfeasible in terms of risk/reward.

### Proprietary Software

!!! info "Some modding tools, like REDMod for Cyberpunk 2077, are proprietary and only available for use on Windows."

These tools cannot be ported to other platforms due to legal restrictions or
technical limitations imposed by their closed-source nature.

## Terminology

- **WINE**: A compatibility layer for running Windows applications on Linux.
- **WINEPREFIX**: A directory that contains a minimal Windows-like Environment.
    - This is a 'virtual' `C:\`, which naturally contains the Registry, Installed Programs etc.
- **Proton**: Valve and CodeWeavers' fork of WINE that is used by Steam to run Windows games on Linux.
    - Proton is Wine with components like [FAudio] and [DXVK] baked in.
    - Used with `Steam Linux Runtime` that provides a stable, known set of native Linux libraries to target.
    - When used in Steam, it automatically creates a **WINEPREFIX** for each game (if not present) on first boot.
- [winetricks]: A Python script to help with the installation of common libraries and tools needed to run some Windows applications like VC Redist, .NET, or extra Windows libraries.
- [`protontricks`][protontricks]: A wrapper for [winetricks] that works with Steam's Proton installations.
- [umu] (a.k.a. ULWGL): A tool to use the `Steam Linux Runtime` + `Proton` without Steam.

## Tricks

!!! info "These are some shortcuts, useful commands and 'gadgets' for development."

### Getting a WINEPREFIX for a given Steam ID

!!! info "By capturing stdout of `protontricks` we can get the WINEPREFIX for a specific AppID."

```bash
protontricks -c 'echo $WINEPREFIX' 2990 2>/dev/null
```

If the operation succeeds, we get just the WINEPREFIX.
If it fails, we get the following stdout:

```
Steam app with the given app ID could not be found. Is it installed, Proton compatible and have you launched it at least once? You can search for the app ID using the following command:
$ protontricks -s <GAME NAME>
```

### Launching a Proton Powered Game

!!! info "To launch via Steam, use `protontricks-launch`"

```
protontricks-launch --appid 213610 "sonic2app.exe"
```

!!! info "To launch via Steam Runtime, but without needing Steam installed use `umu`"

```
WINEPREFIX="/mnt/SharedGames/SteamLibrary/steamapps/compatdata/213610/pfx" GAMEID=0 PROTONPATH="/home/sewer/.local/share/Steam/steamapps/common/Proton 9.0 (Beta)" umu-run sonic2app.exe
```

Use [Getting a WINEPREFIX for a given Steam ID](#getting-a-wineprefix-for-a-given-steam-id)
to get the WINEPREFIX. And look in `steamapps` folder for a Proton install
if you already use Steam.

## Risks (Running Games)

!!! info "These are the user facing risks involved when *manually running games*."

[Further Reading/Research Moved to Separate Gist](https://gist.github.com/Sewer56/3b31857fe85f20fe87d4f2efd988eacf).

## Risks (Running General Tools)

!!! info "These are the user facing risks involved when running general tools."

    That is non-game binaries.

### Missing WINEPREFIX for a given Steam ID

!!! tip "This doesn't seem to apply to newer versions of the Steam Client"

    Newer versions of Steam client appear to automatically create the
    relevant `pfx` folder on game install.

    We might not need to worry about this.

!!! info "If the user has not yet run a game from Steam, we may not have a WINEPREFIX we can use."

- Investigate whether `umu-launcher` can be used to generate a WINEPREFIX.
- If not, we may need to prompt the user to run a game from Steam.

### Missing Runtimes

!!! info "Some tools may require installing additional external runtimes."

[winetricks] can be used to install these runtimes in many cases:

- `dotnet48`: .NET Framework
- `dotnet6`/`dotnet7`/etc. for .NET Core
- `vcrun2022`: Visual C++ 2022 Redistributable (use for MSVC >= 2015 builds)
    - Many games require this, and thus Steam installs this as a post-download step.
    - Manual installation is required in rare case you have a MSVC `< 2015` game requiring a `>= 2015` tool.

Example:

```
WINEPREFIX=/home/sewer/.steam/steam/steamapps/compatdata/213610/pfx winetricks dotnet48
```

#### Auto Detecting Runtime Requirements for Binaries

!!! info "Information on how to detect runtime requirements for certain types of tools."

At some point, we may offer the ability for users to run arbitrary modding
tools installed from the Nexus site.

This will require some additional work, of automatically detecting required missing
runtimes in precompiled binaries so stuff can 'just work'.

[Further Reading/Research Moved to Separate Gist](https://gist.github.com/Sewer56/1b62dc318c643b65e1f2a2a272e374d5).

## Static Compilation of Dependencies

!!! info "Not all the dependencies may be available on all distros."

For example [umu] would be considered universal, but is still not easily acquirable
from environments like Ubuntu Linux. We don't currently plan to static compile
dependencies, therefore the relevant docs were moved to a gist.

[Static Compilation notes for non-.NET Projects Available in the following gist](https://gist.github.com/Sewer56/8a821b07e12e09f53b9ddb5b99c5d22e)

## Planned Steps

Support will be added on a game-by-game basis.

We will add functionality to the App as it's needed.

We don't plan to [Statically Compile Dependencies](#static-compilation-of-dependencies) unless
it proves to be absolutely needed with no alternative way.

### Phase One

!!! info "This gets us basic support for Steam Games, which is what is required"

Detect if [protontricks] is installed.

Warn the user in a modal if it is not.

!!! tip "This is driven by requirement to run `redmod` with Cyberpunk 2077"

### Undetermined

!!! info "Will be done as/when required for each new supported use case"

1. Warn the user pre-deployment if a WINEPREFIX for a given game does not exist.
    - See the [Getting a WINEPREFIX for a given Steam ID](#getting-a-wineprefix-for-a-given-steam-id) section.

2. Auto-detect missing runtimes for tools for a given WINEPREFIX; emitting a warning or diagnostic.

3. Support WINEPREFIX(ES) managed/created by `bottles`, `heroic`, `playnite`, etc.
    - Needs further research and/or input from @erri120

4. Auto-detect various types of tools (.NET Core, .NET Framework, etc.) and offer to install them into WINEPREFIX

5. Handle possible error of missing WINEPREFIX for a given Steam ID.

[steam-api]: https://partner.steamgames.com/doc/api/steam_api
[steam-fix-reboot]: https://reloaded-project.github.io/Reloaded-III/Loader/Copy-Protection/Windows-Steam.html#avoid-forced-reboot
[winetricks]: https://github.com/Winetricks/winetricks
[DXVK]: https://github.com/doitsujin/dxvk
[FAudio]: https://github.com/FNA-XNA/FAudio
[protontricks]: https://github.com/Matoking/protontricks
[umu]: https://github.com/Open-Wine-Components/umu-launcher
[PyInstaller]: https://www.pyinstaller.org/
[IMAGE_DATA_DIRECTORY]: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_data_directory#remarks
[optimized pattern scanner]: https://github.com/Reloaded-Project/Reloaded.Memory.SigScan
[NetCoreInstallChecker]: https://github.com/Sewer56/NetCoreInstallChecker
