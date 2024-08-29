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

## Affected Categories of Tools

!!! info "Which tools do we need to run with a compatibility layer?"

- Built on Windows only Toolkits e.g. WPF
    - Often native ports are not feasible here.
- Some tools are Proprietary
    - *For example REDMod*.
    - These cannot be ported. Sometimes for legal reasons, sometimes for technical reasons.

## Terminology

- **WINE**: A compatibility layer that allows us to run Windows applications on Linux (& other Unix-like OSes).
- **WINEPREFIX**: A directory that contains a WINE environment.
    - A WINEPREFIX can be thought of as a 'bottle' that contains a minimal Windows environment.
    - That is stuff like `Windows Registry`, `Windows DLLs`, `C:\`, `App Installations` etc.
- **Proton**: Valve and CodeWeavers' fork of WINE that is used by Steam to run Windows games on Linux.
    - Proton is Wine with  components like [FAudio] and [DXVK] baked in.
    - Used with `Steam Linux Runtime` that provides a stable, known set of native Linux libraries to target.
    - When used in Steam, it automatically creates a **WINEPREFIX** for each game (if not present) on first boot.
- [winetricks]: A Python script to help with the installation of libraries and tools needed to run some Windows applications.
- [`protontricks`][protontricks]: A wrapper for [winetricks] that allows us to use [winetricks] with Steam's Proton installations.
- [umu] (a.k.a. ULWGL): A tool to use the `Steam Linux Runtime` + `Proton` without Steam.

## Tricks

!!! info "These are some shortcuts, useful commands and 'gadgets' for development."

### Getting a WINEPREFIX for a given Steam ID

!!! info "By capturing stdout of `protontricks` we can get the WINEPREFIX for a specific AppID."

```bash
protontricks -c 'echo $WINEPREFIX' 2990 2>/dev/null
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

[steam-api]: https://partner.steamgames.com/doc/api/steam_api
[steam-fix-reboot]: https://reloaded-project.github.io/Reloaded-III/Loader/Copy-Protection/Windows-Steam.html#avoid-forced-reboot
[winetricks]: https://github.com/Winetricks/winetricks
[DXVK]: https://github.com/doitsujin/dxvk
[FAudio]: https://github.com/FNA-XNA/FAudio
[protontricks]: (https://github.com/Matoking/protontricks)
[umu]: https://github.com/Open-Wine-Components/umu-launcher
