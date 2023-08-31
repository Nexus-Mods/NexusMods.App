## General Info

- Name: Skyrim Legendary Edition
- Release Date: 2011
- Engine: Creation (Gamebryo)

## Uploaded Files Structure

[Using popular mods as examples.]

Uploaded Skyrim LE mods can appear in the following form:

https://www.nexusmods.com/skyrim/mods/13722?tab=files

- Files Targeting Data Subfolder
  - [Unofficial Skyrim Legendary Edition Patch](https://www.nexusmods.com/skyrim/mods/71214?tab=files)
  - Root of archive maps to data.
  - e.g. `/Unofficial Skyrim Legendary Edition Patch.bsa` maps to `Data/Unofficial Skyrim Legendary Edition Patch.bsa`.

- Loose Files Targeting `Data` Subdirectory
    - [Skyrim HD - 2K Textures](https://www.nexusmods.com/skyrim/mods/607?tab=files)

- Files Targeting Game Root Directory
    - [SKSE Plugin Preloader](https://www.nexusmods.com/skyrim/mods/75795?tab=files)

Mods can ship as 'loose files' or '.esp+.bsa' pairs

## Additional Considerations for Manager

### Plugins.txt

Found in: `%LOCALAPPDATA%\Skyrim`.

The `plugins.txt` file in Skyrim lists plugin files with the ".esp" and ".esm" extensions that are loaded after the game's own.

- `.esm` stands for "Elder Scrolls Master" and is mainly used for adding database data other plugins (`.esp`, `.esl`) rely upon; such as models, terrain, mechanics. Mainly used for large scale overhauls, and loaded first before `.esp`(s).
- `.esp` stands for "Elder Scrolls Plugin" and is used for most mods that add new content to the game, i.e. weapons, armor, or quests. They can have dependencies on `.esm`(s).

Each `.esp`/`.esm` has a 'Mod Index':
- Using naming convention `xxYYYYYY`, where `xx` is plugin slot.
- Therefore there is a 255 implicit item limit.

`.esm`(s) `.esp`(s) by the engine.

Typical file looks like:
```txt
Dawnguard.esm
Dragonborn.esm
HearthFires.esm
```


### LoadOrder.txt

Found in: `%LOCALAPPDATA%\Skyrim`.

Typical file looks like:
```txt
Dawnguard.esm
HearthFires.esm
Dragonborn.esm
```

This file declares the order in which all `.esm` and`.esp` files are loaded relative for each other.
It has no effect on the game's runtime (this stores disabled mods, too); it is only used for book keeping.
Usually used for preserving order of mods when re-enabling them once they have been disabled.

With regards to load order in general, item that loads last, wins.
NMA shouldn't need to generate this file.

### Masters (Dependencies)

Plugins (`.esp`), can have 'Masters'; these are effectively dependencies.

To load a given plugin, all masters present in the plugin file's header must also be enabled.
These masters are usually `.esm`, files but can also technically be other `.esp` files.

### BSA Archives

BSAs are a collection of archived files that are loaded either if there is a plugin with the same name

(Unofficial Skyrim Legendary Edition Patch.esp -> Unofficial Skyrim Legendary Edition Patch.bsa)

or if the archive is listed in the `Archive` section of the `Skyrim.ini` file 
(see [sResourceArchiveList](https://stepmodifications.org/wiki/Guide:Skyrim_INI/Archive#sResourceArchiveList)).

The files listed here are loaded first, and then the files from the plugins, in the order listed in plugins.txt.
Later archives overwrite earlier archives.

For list of BSA formats compatible with Skyrim see: [Bethesda mod archives](https://wiki.nexusmods.com/index.php/Bethesda_mod_archives).

### Loose Files Load Order

Files can include loose files that need to be pushed out to `Data` subfolder.
These files will take priority over those packed in `.bsa` archives.

Usually this is only done for texture mods and shouldn't cause issues with other mods;
but it is worth taking into account.

## Essential Mods & Tools

Tooling for this game is built ; i.e. users are expected to modify the game folder directly, and use tools on the game folder.

### [SKSE](https://skse.silverlock.org)

Adds additional scripting capabilities and engine changes not present in vanilla.
Also acts as code loader; loading DLLs from inside `Data/SKSE/Plugins`.

This mod is considered essential; as it is a required dependency for many mods out there.
It should ideally be preinstalled automatically for most mod setups.

### [SKSE Plugin Preloader](https://www.nexusmods.com/skyrim/mods/75795?tab=files)

A DLL stub (entry point: `d3dx9_42.dll`) which forces itself to load before SKSE using DLL Hijacking approach.
This is done as some mods, e.g. [Crash fixes](https://www.nexusmods.com/skyrim/mods/72725) need to kick in before SKSE loads to make engine changes.


### [LOOT](https://loot.github.io)

A Load Order Optimisation Tool (LOOT).
Inspects mod archives and figures out optimal order that plugins `.esp`(s) should be loaded in.

Very [Useful information about load ordering here](https://loot.github.io/docs/help/Introduction-To-Load-Orders.html#:~:text=In%20Skyrim%2C%20the%20load%20order,load%20order%20of%20all%20plugins.).

Considered essential in scope of Skyrim and requires implementation for MVP.

### [xEdit (TES5Edit)](http://tes5edit.github.io)

xEdit is a modding tool for Skyrim that allows you to edit masters, plugins and the
data structured contained within those files.

In the context of mod management, it includes various functions for comparing and merging mods
(merging mods helps preventing you from hitting plugin limits); resolving conflicts
between mods and cleaning up records.

This tool is essential for complex Skyrim mod setups.

### [FNIS](https://www.nexusmods.com/skyrim/mods/11811)

A third-party modding tool that allows for more advanced and complex animations to be used in the game.
Usually you would run this utility after deploying a new mod setup, but before running the game.

It detects supported mods in game folder; autogenerates new files and off it goes.
Ideally we should detect FNIS-supported mods and prompt the user if they want to run FNIS before starting the game.

## Deployment Strategy

Standard deployment: Push files out to game folder.
Not suitable for VFS; tools work on game folder directly, and thus having VFS would affect user experience; this is a no-go.

## Work To Do

[Refer to the relevant Epic for the game.](https://github.com/Nexus-Mods/NexusMods.App/issues/34)

## Misc Notes

Code injection approach is fundamentally flawed; there is a
lack of distinction between 'mod loader' and 'mod'. SKSE tries to be both;
but by being a 'mod' itself, it opens the need for components like preloader to
augment engine logic before the 'mod' part of SKSE kicks in.
