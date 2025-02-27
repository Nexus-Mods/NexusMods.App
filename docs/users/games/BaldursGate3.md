!!! example "We're working on it"
    Baldur's Gate 3 support is currently in development. Get involved by joining us on [GitHub](https://github.com/Nexus-Mods/NexusMods.App/issues/new/choose), [Discord](https://discord.gg/ReWTxb93jS) or the [forums](https://forums.nexusmods.com/forum/9052-nexus-mods-app/)!

## Features
In addition to basic mod management features, players also benefit from these dedicated features:

### Loadout Health Check
Get information on potential issues in your loadout(s). [Learn more about Health Checks.](../features/HealthCheck.md)

Diagnostics are shown in the following situations: 

- A mod is installed and requires another mod which is not installed or enabled. This check uses the meta.lsx file inside the PAK. 
- A mod is installed which requires BG3 Script Extender but it is not installed.
- On Linux, BG3 Script Extender is installed via WINE but the WINE DLL Override setting is not set to allow the correct DLL file to be used.

### Selective Game Backup
When managing Baldur's Gate 3, the app will back up only the core game files (default) or the entire game folder. Backing up the whole game requires significantly more hard drive space. This option can be toggled in the :material-cog: Settings menu.


## Compatibility
This game can be managed via the app on the following operating systems and platforms. The application will automatically detect valid installations from supported game stores if possible. 

|| :fontawesome-brands-windows: Windows |  :fontawesome-brands-linux: Linux | :fontawesome-brands-apple: macOS |
|---|---|---|---|
| :fontawesome-brands-steam: [Steam](https://store.steampowered.com/app/1086940/Baldurs_Gate_3/) | :material-check-circle: | :material-check-circle: | :material-close-thick: |
| <img src="../../images/GOG.com_logo_white.svg" alt="GOG" width="14"/> [GOG](https://www.gog.com/en/game/baldurs_gate_iii) | :material-check-circle:[^1] | :material-check-circle:[^1][^2] | :material-close-thick: |

[^1]: Offline backup installs from GOG.com cannot be detected automatically.
[^2]: [Heroic Launcher](https://heroicgameslauncher.com/) is required. 
