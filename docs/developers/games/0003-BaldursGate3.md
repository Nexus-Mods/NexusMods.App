## Baldur's Gate 3

## Platforms and Stores:
- Windows (Steam, GOG)
- Linux (Wine) (Steam, GOG)
- MacOS (Steam, GOG)

## Engine and Mod Support
Baldur's Gate 3 uses the Divinity 4.0 engine, which is a modified version of the Divinity 3.0 engine used in Divinity: Original Sin 2.
BG3 was released in Early Access in 2020, and thanks to the similarities with DOS2, which had official modding support, mods started appearing since then.
BG3 has native Windows and MacOS support, but Linux users can play it using Wine.

BG3 added official modding support through ModIo in Patch 7 (sep 2024), but majority od mods are still available on NexusMods.
BG3 offers an in-game mod manager, to download, toggle and remove mods from ModIo. There is no Load Order management support as of Patch 7. 
"Third party" mods are still recognized and used by the game, but they are not officially supported.

## Game Files and Locations
### Windows/Wine: 
Two executables: `bg3.exe` and `bg3_dx11.exe` in `Baldurs Gate 3/Bin`. One for Vulkan, one for DirectX 11.
Game settings and load order are stored in `%localappdata%\Larian Studios\Baldur's Gate 3`.
Majority of mods are stored in `%localappdata%\Larian Studios\Baldur's Gate 3\Mods`.
Load order is stored in `%localappdata%\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\modsettings.lsx`.

### MacOS:
TBD

## Mod formats:
### BG3 Script Extender (BG3SE)
BG3SE is a modding framework for BG3, similar to SKSE for Skyrim. It comes in the form of a `DWrite.dll` to be placed in the game's `Bin` folder.

This dll is actually just the installer/updater of the extender, which installs itself in `%AppData%\Local\BG3ScriptExtender`.
The app should not need to manage that folder, as the extender will update itself when needed.

### `.pak` mods
BG3 uses `.pak` archives as the main format for mods. 
These pak archives should be installed in the `%localappdata%\Larian Studios\Baldur's Gate 3\Mods` folder.



## Additional Considerations for Manager

## Essential Mods & Tools

## Deployment Strategy

## Work To Do

## Misc Notes
