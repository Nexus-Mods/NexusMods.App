# Darkest Dungeon

## Stores and Ids

- [Steam](https://store.steampowered.com/app/262060/Darkest_Dungeon): `262060`
- [GOG](https://www.gog.com/game/darkest_dungeon): `1450711444`
- [Epic Games Store](https://store.epicgames.com/en-US/p/darkest-dungeon): [`b4eecf70e3fe4e928b78df7855a3fc2d`](https://erri120.github.io/egs-db/namespaces/21916c391ae4425d8f6cce2382aebd0c/index.html) (NOTE: API didn't return anything...)
- [Xbox Game Pass](https://www.xbox.com/en-US/games/store/p/bxkvn6g9c5wj): `UNKNOWN`

## Engine

Darkest Dungeon uses a home-rolled lightweight cross-platform engine ([source](https://www.gamedeveloper.com/audio/road-to-the-igf-red-hook-studios-i-darkest-dungeon-i-)) and runs natively on desktop (Windows/Linux/macOS) handhelds (PlayStation Vita/Nintendo Switch) and consoles (PlayStation 4/Xbox One/Xbox Series X|S).

## Game Files

### Executables

- Windows: `_windowsnosteam/Darkest.exe` (all Stores), `_windows/Darkest.exe` (only Steam)
- Linux: `_linuxnosteam/darkest.bin.x86`/`_linuxnosteam/darkest.bin.x86_64` (all Stores), `_linux/darkest.bin.x86`/`_linux/darkest.bin.x86_64` (only Steam)
- macOS: `_osxnosteam/Darkest.app/Contents/MacOS/Darkest NoSteam` (all Stores), `_osx/Darkest.app/Contents/MacOS/Darkest` (only steam)

Darkest Dungeon on Steam ships with **both** the version that uses Steam (eg: `_windows/Darkest.exe`) and the version that doesn't use Steam (eg: `_windowsnosteam/Darkest.exe`). Other Stores, like GOG, only come with the `nosteam` version. Furthermore, on Linux, the game also comes with a 32-bit and 64-bit executable:

```txt
file _linux/darkest.bin.x86

_linux/darkest.bin.x86: ELF 32-bit LSB executable, Intel 80386, version 1 (SYSV), dynamically linked, interpreter /lib/ld-linux.so.2, for GNU/Linux 3.2.0, BuildID[sha1]=86f62b1e488e55040d6555280c0ae21e8868149e, not stripped
```

```txt
file _linux/darkest.bin.x86_64

_linux/darkest.bin.x86_64: ELF 64-bit LSB executable, x86-64, version 1 (SYSV), dynamically linked, interpreter /lib64/ld-linux-x86-64.so.2, for GNU/Linux 3.2.0, BuildID[sha1]=84fdc9d65de45bb814cb51c83a422fd3ec5a5381, not stripped
```

### Saves

For non-Steam versions, saves are located in the `Darkest` sub-directory of the documents folder:

- Windows: `%USERPROFILE%\Documents\Darkest`
- Linux: `${XDG_DOCUMENTS_DIR:-$HOME/Documents}/Darkest`

For Steam versions, cloud saves are used instead: `{STEAM_PATH}/userdata/{USERID}/{GAMEID}/remote/`.

The game also saves a `persist.options.json` inside the local application directory:

- Windows: `%LOCALAPPDATA%\Red Hook Studios\Darkest\persist.options.json`
- Linux: `${XDG_DATA_HOME:-$HOME/.local/share}/Red Hook Studios/Darkest/persist.options.json`

This file contains global game settings that are save in-dependent (eg: Language, Volume and Graphics Settings).

#### Save file Format

Although the extension for save related files are `.json`, the actual file contents are _often_ binary. The [Darkest Dungeon Save Editor](https://github.com/robojumper/DarkestDungeonSaveEditor) contains a Java and Rust implementation and also hosts a website: https://robojumper.github.io/DarkestDungeonSaveEditor.

## Mod Support

The game developers published an official modding guide on Steam: [Darkest Dungeon - Modding Guide \[Official\]](https://steamcommunity.com/sharedfiles/filedetails/?id=819597757). As of writing this document, the guide hasn't been updated since 10 Jul, 2017, however it's still relevant.

Due to the nature of it's engine, the game is very open to modding. The game doesn't use archives nor custom-made formats or languages. Non-art asset files are stored as text files (`.darkest`, `.json`, `.xml`), images are stored as PNGs and and most character art assets are created using [Spine](https://esotericsoftware.com/). As such, every single file can easily be changed leading to easy mod installation **and** mod creation.

### Native Mod support

The game has native mod support using the `mods` sub-directory. Instead of overwriting loose game files, mods can mirror the game folder structure and create sub-directories inside the `mods` folder for each mod:

```text
mods
├── NoNegQuirkOnSuccess
│       ├── project.xml
│       └── shared
│           └── nonegquirk.rules.json
```

Each mod **requires** a `project.xml` file, which describes the mod in detail:

```xml
<?xml version="1.0" encoding="utf-8"?>
<project>
    <Title>My dummy mod</Title>
    <Language>english</Language>
    <ItemDescription>This is a very cool mod.</ItemDescription>
    <PreviewIconFile>preview_icon.png</PreviewIconFile>

    <VersionMajor>1</VersionMajor>
    <VersionMinor>0</VersionMinor>
    <TargetBuild>0</TargetBuild>

    <!-- Path to the mod data files, only needed when using the Steam Workshop Uploader -->
    <ModDataPath/>

    <!-- Steam Workshop Tags -->
    <Tags>
        <Tags>Tag 1</Tags>
        <Tags>Tag 2</Tags>
    </Tags>

    <!-- Visibility on the Steam Workshop -->
    <!-- Values: public, private, friends -->
    <Visibility>private</Visibility>

    <!-- Upload mode, only used for the Steam Workshop Uploader -->
    <!-- direct_upload: default value when using the Uploader -->
    <!-- dont_submit: Uploader will generate additional files but doesn't actually upload the mod -->
    <UploadMode>direct_upload</UploadMode>

    <!-- Auto-generated by the Steam Workshop Uploader. This is a unique ID for the mod on Steam. -->
    <!-- https://steamcommunity.com/sharedfiles/filedetails/?id=2896035307 has the PublishedFileId 2896035307 -->
    <PublishedFileId>0</PublishedFileId>

    <!-- Unused -->
    <ItemDescriptionShort/>
    <UpdateDetails/>
</project>
```

This `project.xml` file can be created manually, or with the Steam Workshop Uploader. This uploader is only available on Windows at `_windows\steam_workshop_upload.exe` and it will populate the file with auto-generated values, like the `PublishedFileId`. This Uploader makes it very easy to create, manage and upload mods, as it also generates a `modfiles.txt` file and localization files.

### Priority Order

After installing some mods using the Steam Workshop, or by manually downloading and extracting the files to the `mods` folder, these mods can be enabled in the game for a specific save slot. The "load order" dictates which mods are loaded **last**:

- Mod A (loads last)
- Mod B (loads after C and before A)
- Mod C (loads first)

The term "Load Order" might be a misnomer and "Priority Order" might fit better, as the highest mod in the list has the highest priority and can overwrite mods with lower priority.

Since this "Priority Order" is specific to a save slot, it will be saved along side the save data in the `persist.game.json` file (other properties omitted):

```json
{
  "base_root": {
    "applied_ugcs_1_0": {
      "0": {
        "name": "2896035307",
        "source": "Steam"
      },
      "1": {
        "name": "2087063447",
        "source": "Steam"
      },
      "2": {
        "name": "My dummy mod",
        "source": "mod_local_source"
      }
    },
    "persistent_ugcs": {
      "applied_ugcs_1_0": {
        "0": {
          "name": "2896035307",
          "source": "Steam"
        },
        "1": {
          "name": "2087063447",
          "source": "Steam"
        },
        "2": {
          "name": "My dummy mod",
          "source": "mod_local_source"
        },
        "3": {
          "name": "I removed this mod",
          "source": "mod_local_source"
        }
      }
    }
  }
}
```

The `applied_ugcs_1_0` is the _current_ list of mods that are enabled, while `persistent_ugcs` tracks _all_ mods that have been enabled at some point. Each value is an object where `source` can either be `Steam` for Steam Workshop mods or `mod_local_source` for manually installed mods. The `name` value is the `PublishedFileId` value for Steam Workshop mods and the `Title` value for manually installed mods.

### Mod Conflicts

#### Assets

Asset conflicts are very straight forward: Mod A and Mod B provide file X, this conflict can be resolved using the [Priority Order](#priority-order) or removing the file from one of the mods.

#### Variables and Game Mechanics

Although the game files are very open and you can easily modify anything, the mod loading mechanics are very simplified, in that they can only load _files_. As an example, let's take the Stage Coach building upgrades file:

```json5
// Game/stage_coach.building.json
{
  "number_of_recruits_upgrades": [{ "amount": 2 }],
  "roster_size_upgrades" : [{ "amount" : 9 }],
}
```

If Mod A wants to change the number of recruits you get for level 1 from `2` to `5`, it would have to copy the entire file to its mod directory:

```json5
// Mod A/stage_coach.building.json
{
  // changed from 2 to 5
  "number_of_recruits_upgrades": [{ "amount": 5 }],
  "roster_size_upgrades" : [{ "amount" : 9 }],
}
```

If Mod B wants to change max roster size for level 1 from `9` to `20`, it would also have to copy the entire file to its mod directory:

```json5
// Mod B/stage_coach.building.json
{
  "number_of_recruits_upgrades": [{ "amount": 2 }],
  // changed from 9 to 20
  "roster_size_upgrades" : [{ "amount" : 20 }],
}
```

When you install both mods, there is now a conflict for `stage_coach.building.json` because both mods overwrite the base file. The solution is to create a Mod C, which _merges_ both mods:

```json5
// Mod C/stage_coach.building.json
{
  // Mod A: changed from 2 to 5
  "number_of_recruits_upgrades": [{ "amount": 5 }],
  // Mod B: changed from 9 to 20
  "roster_size_upgrades" : [{ "amount" : 20 }], 
}
```

Another issue comes from the fact that the mod has to copy the game files. If the game updates this file in a patch, the mod has to be updated or else it will just overwrite the file again, potentially breaking the game and essentially reverting the patch. Older mods that haven't been updated in a while suffer greatly from this issue.

This is an area where modern modding tools could really improve the modding experience for Darkest Dungeon.

#### Skins

Name conflicts for _new_ content, like skins, can unintentionally arise. Skins for heroes are some of the most popular mods and there are typically two types of skin mods: re-skins and re-works. A re-skin is simply a new set of images:

Using [Female Flagellant](https://www.nexusmods.com/darkestdungeon/mods/670) (SFW) or [Triss Merigold](https://www.nexusmods.com/darkestdungeon/mods/742?tab=files) as an example, this first mod adds two new skins for the Flagellant. This is done by creating a new sub-directory for each new skin in the main heroes' folder:

```text
.
└── flagellant
    ├── flagellant_A
    ├── flagellant_B
    ├── flagellant_C
    ├── flagellant_D
    ├── flagellant_E
    ├── flagellant_F
```

Each skin has it's own folder and a unique suffix. By default, each hero comes with 4 skins: `A` through `D`. The mod now adds skins `E` and `F`. A mod conflict gets created when two mods share the same suffix for a skin. This can easily be fixed by renaming the folder. The reddit user [u/Anti_Cow](https://www.reddit.com/user/Anti_Cow/) discovered another easy way to fix these conflicts: [post](https://www.reddit.com/r/darkestdungeon/comments/ccp2z5/skin_name_conflict_for_heroes_is_gone/). It turns out that the engine supports more than just suffixed from `A-Z`, but also arbitrary prefixes: `[WHATEVER][CLASSNAME]_[A-Z]`. This means that newer skin mods won't result in conflicts, however older mods that haven't been updated might still have compatability issues. These can be easily identified and fixed manually or automatically.

The second type of skin mod is an entire re-work. These skins come with their own set of animations, effects and skeletons, making them incompatible with every other skin mod for that hero. The [Saber Skin](https://www.nexusmods.com/darkestdungeon/mods/1435) is a good example, as it completely overwrites the game files for the Crusader hero.

## Deployment

Most mods use the native Darkest Dungeon mod format and provide a `project.xml` file that can be parsed. These mods can go directly into the `mods` folder. However, some mods, especially older mods, don't use this mod format and instead are just loose files packaged together. A relevant `project.xml` file can be created dynamically, but they still might not load correctly. That is due to the required file structure of these mods:

The [Female Flagellant](https://www.nexusmods.com/darkestdungeon/mods/670) (SFW) mod mentioned previously is a good example for this. It doesn't contain a `project.xml` file and is just a bunch of loose files packaged correctly. If you were to create a `project.xml` manually or automatically, the mod would not load because the files for the flagellant are located in `dlc/580100_crimson_court/features/flagellant/heroes/flagellant`, so the files would have to be moved to this directory:

```txt
Before:
.
├── flagellant_E
├── flagellant_F

After:
Female Flagellant
├── project.xml
├── dlc
    └── 580100_crimson_court
        └── features
            └── flagellant
                └── heroes
                    └── flagellant
                        └── flagellant_E
                        └── flagellant_F
```

This directory re-structuring can possible be done automatically by analyzing the existing mod structure and selecting the right output directory.

## Community

- [Darkest Dungeon Discord](https://discord.com/invite/darkestdungeon)

## Additional Notes

Manually installing mods on Linux is currently problematic, forcing Proton in Steam is recommended for now until the issue is fixed. The game doesn't correctly parses the `project.xml` files or has other loading issues for **manual mods only**:

- Windows/Proton: https://media.discordapp.net/attachments/716828019565658163/1107633599773159444/image.png
- Native Linux: https://media.discordapp.net/attachments/716828019565658163/1107633600196775996/image.png
- installing a Steam Workshop mod manually on Linux: https://media.discordapp.net/attachments/716828019565658163/1107635098498637844/image.png
