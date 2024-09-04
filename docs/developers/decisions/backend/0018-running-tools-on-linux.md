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

***Windows-Specific Development Frameworks***

Some modding tools are built using Windows-only like WPF (Windows Presentation Foundation)
for the UI.

Porting these tools can sometimes be expensive/unfeasible in terms of risk/reward.

***Proprietary Software***

Some modding tools, like REDMod for Cyberpunk 2077, are proprietary and only
available for Windows.

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

!!! info "These are the user facing risks involved when running games."

### Steam DRM

!!! info "Some Steam games may not allow us to run without via Steam binary/client."

Specifically, the Valve developer documents [recommend game developers to restart][steam-api]
their game if it cannot establish a connection with the Steam client.

While this can be bypassed with some [hooking magic][steam-fix-reboot], we're not
a code injection framework, rather a glorified file manager.

!!! warning "Before starting certain games we should ensure Steam is running."

!!! note "I (Sewer) have had difficulties running Steam API games outside of Proton."

    Here are the exact situations:

    - Running with WINE: game reboots, (presumably) trying to restart via Steam. Nothing happens.
        - e.g. `wine "sonic2app.exe"`
    - Running with WINE (using Proton Prefix): fails with missing steam DLLs
        - e.g. `WINEPREFIX=/home/sewer/.steam/steam/steamapps/compatdata/213610/pfx wine "sonic2app.exe"`
        - `0024:err:module:import_dll Library tier0_s.dll`
        - `0024:err:module:import_dll Library vstdlib_s.dll`
        - Is Steam adding additional DLL directories to WINE?
    - Running with Proton (protontricks)
        - e.g. `protontricks-launch --appid 213610 "sonic2app.exe"`
        - Works fine if Steam client is running.
        - If Steam not running `Application load error P:0000065432` (Steam not running)
            - `[S_API] SteamAPI_Init(): SteamAPI_IsSteamRunning() did not locate a running instance of Steam.`
    - Running with `umu` (formerly ULWGL)
        - `WINEPREFIX="/mnt/SharedGames/SteamLibrary/steamapps/compatdata/213610/pfx" GAMEID=0 PROTONPATH="/home/sewer/.local/share/Steam/steamapps/common/Proton 9.0 (Beta)" umu-run sonic2app.exe`
        - Works fine if Steam client is running.

    Note: I (Sewer) used `Sonic Adventure 2` as an example, but this applies to
    all games that use the Steam API.

### Missing GameOverlayRenderer (Steam Overlay)

!!! warning "Launching outside of Steam will lead to `gameoverlayrenderer64.so` not being injected."

    That's the `Steam Overlay`, for reference.

I'm not sure if there are any ways other than starting via Steam. Loading it manually
might involve some shenanigans with `LD_PRELOAD` and `LD_LIBRARY_PATH`.

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

We can use [winetricks] to install these runtimes in many cases:

- `dotnet48`: .NET Framework
- `dotnet6`/`dotnet7`/etc. for .NET Core
- `vcrun2022`: Visual C++ 2022 Redistributable (use for MSVC >= 2015 builds)
    - Normally you wouldn't need this with `Proton` installed games.
    - Because it'll get installed by Steam with the game.

Example:

```
WINEPREFIX=/home/sewer/.steam/steam/steamapps/compatdata/213610/pfx winetricks dotnet48
```

!!! tip "It might be a good idea to detect runtimes during the download analysis stage"

    This way we can easily cache the results.

#### Detecting .NET Framework

!!! info "Sometimes .NET Framework powered tools are still used for some games"

    Although old, some older tools still rely on this technology, so we must try
    to detect if the user has it installed.

To detect if an application requires .NET Framework, look at the [IMAGE_DATA_DIRECTORY]
structure of the PE header. If the `The CLR header address and size` field is present
and not 0; then the application requires .NET Framework.

!!! info "To detect if .NET Framework is installed on the system, check the following directory."

    `"C:\Windows\Microsoft.NET\Framework"`

    There should be a folder for each version. However, 4.X are backwards compatible,
    and thus only 4.8.2 (or latest) is required.

!!! note "We're unlikely to run into this scenario in the immediate future"

    Therefore no plan of action is currently to be done.

#### Detecting .NET Core

!!! warning "This can be a bit tricky."

There are multiple ways to package a .NET Application:

- Self Contained Deployment
- Single File Deployment (non NativeAOT)
- Framework Dependent Deployment

For the purposes of our use case, self-contained and framework dependent work
the same, however self-contained single file deployment differs slightly.

##### Framework Dependent Deployment

Consider a typical application:

```
- `apphost.exe`
- `apphost.runtimeconfig.json`
- `apphost.deps.json`
```

Replace `apphost` with name of EXE.

When a .NET Core application is built, the SDK copies `apphost.exe` and amends
its resources with the name, icon, publisher of the final target application.

This in turn searches for a corresponding `dll` file matching the name of the exe.

Simply seeing `.runtimeconfig.json` and `.deps.json` will guarantee a matching
.NET Core application built with apphost. They are *technically optional*
but most devs are unaware, and thus won't delete them.

!!! note "An alternative way"

    For a more reliable way that accounts for the deleted files, look at the
    PE header of the `DLL` file and look for the CLR header, as per
    [Detecting .NET Framework](#detecting-net-framework) section.

To detect if .NET Core is installed on the system, use [NetCoreInstallChecker],
for support for checking a `WINEPREFIX`, library will need a small PR for setting
alternative root (`C:`) folder.

##### Single File Deployment

!!! info "The easiest way is to search the binary for the pattern below"

```csharp
0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
```

This set of bytes is SHA-256 hash for the string `".net core bundle"`.

This can be done at around 60GB/s on a single thread of a 5900X with an [optimized pattern scanner].

Unfortunately this alone does not determine if the Single File deployment is self-contained or not.
To determine if it's self-contained, check the PE header for an export named `DotNetRuntimeInfo`.

## Static Compilation of Dependencies

!!! info "Not all the dependencies may be available on all distros."

For example [umu] would be considered universal, but is still not easily acquirable
from environments like Ubuntu Linux.

### Python

!!! info "This applies to [umu], [protontricks] and [winetricks]."

Static bundling of Python executables can be done with [PyInstaller].

Set up a virtual environment and enable it:

```
python -m venv protontricks
source protontricks/bin/activate
```

Install [PyInstaller] with:

```bash
pip install pyinstaller
```

Then clone the project:

```bash
git clone https://github.com/Matoking/protontricks.git
cd protontricks
```

Install the project from disk:

```bash
# This gets you the dependencies, and lets you test a local build
# before bundling.
pip install --editable .
```

Locate the `def main()` function, and build with [PyInstaller]:

```bash
# The `add-data` flag allows us to bundle additional files with the executable,
# in this case we want the `data` folder, which will be output to the `data`
# folder when bundled. For protontricks, this folder contains useful bash scripts.
pyinstaller --onefile src/protontricks/cli/main.py --name protontricks --add-data "src/protontricks/data:protontricks/data"
```

This should have output the binary in the `dist` folder, we can run it like this:

```bash
./dist/protontricks --help
```

#### Troubleshooting

!!! info "The above sequence of commands usually works, but sometimes, like in the case above it doesn't."

For `protontricks` specifically, we get the following error:

```
Traceback (most recent call last):
  File "protontricks/cli/main.py", line 15, in <module>
ImportError: attempted relative import with no known parent package
[PYI-654761:ERROR] Failed to execute script 'main' due to unhandled exception!
```

This is because `protontricks` uses relative imports from parent directories, and
[PyInstaller] is unable to resolve these dependencies. To workaround this limitation,
you can create a simple script outside of the package (i.e. `src/entry.py`) that
imports and calls the `cli` function to serve as an entrypoint:

```python
from protontricks.cli.main import cli

if __name__ == "__main__":
    cli()
```

The path to this script can then be passed to [PyInstaller] to build the binary:

```bash
pyinstaller --onefile src/entry.py --name protontricks --add-data "src/protontricks/data:protontricks/data"
```

##### Running Module Locally

!!! info "I ran into issues running the module locally with `python -m protontricks.cli.main`"

In my case I needed to make a patch from:

```python
if __name__ == "__main__":
    main()
```

to

```python
if __name__ == "__main__":
    main(None)
```

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
