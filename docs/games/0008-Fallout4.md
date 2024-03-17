## General Info

- Name: Fallout 4
- Release Date: 2015
- Engine: Creation (Gamebryo)

## Uploaded Files Structure

!!! info "Using popular mods as examples."

Uploaded Fallout 4 mods can appear in the following form:

- Files Targeting Data Subfolder
    - [Unofficial Fallout 4 Patch](https://www.nexusmods.com/fallout4/mods/4598?tab=files)
    - Root of archive maps to data.
    - e.g. `/Unofficial Fallout 4 Patch - Main.ba2` maps to `Data/Unofficial Fallout 4 Patch - Main.ba2`.

- Loose Files Targeting `Data` Subdirectory
    - [Commonwealth Cuts - KS Hairdos - ApachiiSkyHair](https://www.nexusmods.com/fallout4/mods/11402?tab=files)

- Files Targeting Game Root Directory
    - [xSE PluginPreloader F4](https://www.nexusmods.com/fallout4/mods/33946?tab=files)
  
Mods can ship as 'loose files' or '.esp+.ba2' pairs.

## Additional Considerations for Manager

### DLCList.txt

!!! info "Found in: `%LOCALAPPDATA%\Fallout4` after running the game for the first time."

The `DLCList.txt` file in Fallout 4 lists plugin files with the ".esp" and ".esm" extensions that are loaded after the game's own.

- `.esm` stands for "Elder Scrolls Master" and is mainly used for adding database data other plugins (`.esp`, `.esl`) rely upon; such as models, terrain, mechanics. Mainly used for large scale overhauls, and loaded first before `.esp`(s).
- `.esp` stands for "Elder Scrolls Plugin" and is used for most mods that add new content to the game, i.e. weapons, armor, or quests. They can have dependencies on `.esm`(s).
- `.esl` stands for "Elder Scrolls (Master) Light". These are effectively `.esm`(s) with limitations.

Example of file looks like:
```txt
*Unofficial Fallout 4 Patch.esp
*HUDFramework.esm
*WorkshopFramework.esm
BetterRoboticsDisposal.esl
*WorkshopPlus.esp
```

`*` denotes a file that is enabled; and will be loaded by the game, if missing, the game will ignore the plugin.
This makes the presence of loadorder.txt superfluous, but could still be used to keep track of ghosted plugins.

Each `.esp`/`.esm` has a 'Mod Index':

- Using naming convention `xxYYYYYY`, where `xx` is plugin slot.
- Therefore there is a 255 implicit item limit.
- However... slot `0xFE` is reserved for `.esl` files.

The newer `.esl` files have a limit of 4096 items; and have their own limit.

- They have naming convention `FExxxYYY` where `xxx` is plugin slot.
- Therefore there is a 4096 limit.
- i.e. They use the 0xFE slot previously reserved for `.esp` & `.esm`.

`.esm`(s) and `.esl`(s) are always loaded before `.esp`(s) by the engine.

Please note however. ***The game engine has a 512 open file handle limit;***
so you can never realistically consume all possible ~4350 items without hitting this cap.

This can however be increased with [Buffout 4](#buffout-4).

### Masters (Dependencies)

Plugins (`.esp`, `.esl`), can have 'Masters'; these are effectively dependencies.

To load a given plugin, all masters present in the plugin file's header must also be enabled.
These masters are usually `.esm`, files but can also technically be other `.esp` files.

### BA2 Archives

BA2s are a collection of archived files that are loaded either if there is a plugin with the same name followed by ` - Main`

`Unofficial Fallout 4 Patch.esp` -> `Unofficial Fallout 4 Patch - Main.ba2`

or by ` - Textures`:

`Unofficial Fallout 4 Patch.esp` -> `Unofficial Fallout 4 Patch - Textures.ba2`

or if the archive is listed in `SResourceArchiveList` in the `Archive` section of the `Fallout4.ini` file.

### Loose Files Load Order

Files can include loose files that need to be pushed out to `Data` subfolder.
These files will take priority over those packed in `.ba2` archives.

Loose files are only loaded by the game if the following lines are present under the `[Archive]` section in `Fallout4Custom.ini`:

```ini
bInvalidateOlderFiles=1
sResourceDataDirsFinal=
```

## Essential Mods & Tools

Tooling for this game is built ; i.e. users are expected to modify the game folder directly, and use tools on the game folder.

### [F4SE](https://f4se.silverlock.org/)

Adds additional scripting capabilities and engine changes not present in vanilla.
Also acts as code loader; loading DLLs from inside `Data/F4SE/Plugins`.

This mod is considered essential; as it is a required dependency for many mods out there.
It should ideally be preinstalled automatically for most mod setups.

### [xSE PluginPreloader F4](https://www.nexusmods.com/fallout4/mods/33946)

A DLL stub (entry point: `IpHlpAPI.dll`) which forces itself to load before F4SE using DLL Hijacking approach.
This is done as some mods, e.g. [Buffout 4](#buffout-4) need to kick in before F4SE loads to make engine changes.

### [Buffout 4](https://www.nexusmods.com/fallout4/mods/47359)

General suite of fixes for game code/logic.

This one is considered essential for large mod setups because the ***game engine has a 512 open file handle limit;*** which means in practice you're hard capped at around 400-500 mods. This mod can extend that limit to 2048.

### [LOOT](https://loot.github.io)

A Load Order Optimisation Tool (LOOT).
Inspects mod archives and figures out optimal order that plugins `.esp`(s) should be loaded in.

Very [Useful information about load ordering here](https://loot.github.io/docs/help/Introduction-To-Load-Orders.html).

Considered essential in scope of Fallout 4 and requires implementation for MVP.

### [xEdit (FO4Edit)](http://tes5edit.github.io)

xEdit is a modding tool for Fallout 4 that allows you to edit masters, plugins and the
data structured contained within those files.

In the context of mod management, it includes various functions for comparing and merging mods
(merging mods helps preventing you from hitting plugin limits); resolving conflicts
between mods and cleaning up records.

This tool is essential for complex Fallout 4 mod setups.

## Deployment Strategy

Standard deployment: Push files out to game folder.

Not suitable for VFS; tools work on game folder directly, and thus having VFS would affect user experience; this is a no-go.

## Work To Do

[Refer to the relevant Epic for the game.](https://github.com/Nexus-Mods/NexusMods.App/issues/37)

## Misc Notes

Code injection approach is fundamentally flawed; there is a
lack of distinction between 'mod loader' and 'mod'. F4SE tries to be both;
but by being a 'mod' itself, it opens the need for components like preloader to
augment engine logic before the 'mod' part of F4SE kicks in.