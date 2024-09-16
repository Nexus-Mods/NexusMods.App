Use the Nexus Mods app to manage your Cyberpunk 2077 mods!

!!! example "We're working on it"
    Cyberpunk 2077 support is currently in development. Get involved by joining us on [GitHub](https://github.com/Nexus-Mods/NexusMods.App/issues/new/choose), [Discord](https://discord.gg/ReWTxb93jS) or the [forums](https://forums.nexusmods.com/forum/9052-nexus-mods-app/)!

## Features
In addition to basic mod management features, players also benefit from these dedicated features:

### Loadout Health Check
Get information on potential issues in your loadout(s). [Learn more about Health Checks.](../features/HealthCheck.md)

![An example Health Check message prompting the installation of a missing mod.](../images/0.6.0/HealthCheckExampleCyberpunk.webp)

Diagnostics are shown in the following situations: 

- A mod is installed and requires any of the following dependencies by they are not installed or enabled:
    - [Codeware](https://www.nexusmods.com/cyberpunk2077/mods/7780) - Detected by `.reds` files or Lua scripts with the `Codeware` global. 
    - [Virtual Atelier](https://www.nexusmods.com/cyberpunk2077/mods/2987) - Detected by `.reds` files uses hooks for this mod.
    - [Appearance Menu Mode](https://www.nexusmods.com/cyberpunk2077/mods/790) - Detected by Lua scripts in the `bin/x64/plugins/cyber_engine_tweaks/mods/AppearanceMenuMod/Collabs` folder.
    - [RED4Ext](https://www.nexusmods.com/cyberpunk2077/mods/2380) - Detected by `.dll` files in the `red4ext/plugins` folder.
    - [TweakXL](https://www.nexusmods.com/cyberpunk2077/mods/4197) - Detected by `.tweak` files in the `r6/tweaks` folder.
    - [ArchiveXL](https://www.nexusmods.com/cyberpunk2077/mods/4198) - Detected by `.xl` files in the `archives` folder. 
    - [Cyber Engine Tweaks](https://www.nexusmods.com/cyberpunk2077/mods/107) - Detected by `.lua` files in the `bin/x64/plugins/cyber_engine_tweaks` folder. 

### Automated deployment for REDmods

!!! warning "This feature currently isn't available on Linux."

When mods using the official REDmod system are installed, it may be necessary to run the the REDmod tool. The app will detect when this is necessary and run it automatically before starting the game. 

### Selective Game Backup
When managing Cyberpunk 2077 the app will back up only the core game files (default) or the entire game folder. Backing up the whole game requires significantly more hard drive space. This option can be toggled in the :material-cog: Settings menu.

## Compatibility
This game can be managed via the app on the following operating systems and platforms. The application will automatically detect valid installations from supported game stores if possible. 

|| :fontawesome-brands-windows: Windows |  :fontawesome-brands-linux: Linux | :fontawesome-brands-apple: macOS |
|---|---|---|---|
| :fontawesome-brands-steam: [Steam](https://store.steampowered.com/app/1091500/Cyberpunk_2077/) | :material-check-circle: | :material-check-circle: | :material-close-thick: |
| <img src="../../images/GOG.com_logo_white.svg" alt="GOG" width="14"/> [GOG](https://store.steampowered.com/app/1091500/Cyberpunk_2077/) | :material-check-circle:[^1] | :material-close-thick:[^1] | :material-close-thick: |
| <img src="../../images/epic-games.svg" alt="Epic Games Store" width="14"/> [Epic Games Store](https://store.epicgames.com/en-US/p/cyberpunk-2077) | :material-check-circle: | :material-close-thick: | :material-close-thick: |

[^1]: Offline backup installs from GOG.com cannot be detected automatically.
