## Baldur's Gate 3

## Platforms and Stores:
- Windows (Steam, GOG)
- Linux (Wine) (Steam, GOG)
- MacOS (Steam, GOG)

## Engine and Mod Support
Baldur's Gate 3 uses the Divinity 4.0 engine, which is a modified version of the Divinity 3.0 engine used in Divinity: Original Sin 2.
BG3 was released in Early Access in 2020, and thanks to the similarities with DOS2, which had official modding support, mods started appearing since then.
BG3 has native Windows and MacOS support, but Linux users can play it using Wine.



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

Pak mods that require BG3SE will usually contain some files under a `ScriptExtender` directory inside the `.pak` archive.

### Loose files mods (data)
These are pretty uncommon, mostly texture replacers. They are installed in the `Game/Data` folder.

These mods have the potential to have file conflicts, as they all install in the Data folder.
E.g. two tattoo replacer mods might have collisions on some textures.

Conflicts are pretty rare, 1 in ~100 mods.

### Pak (`.pak`) mods
BG3 uses `.pak` archives as the main format for mods. 
These should be installed in the `%localappdata%\Larian Studios\Baldur's Gate 3\Mods` folder.

Pak archives are just loose files packaged into an archive with a specific structure.
Each pak archive should contain a `meta.lsx` file, which is a XML file with important metadata for the mod.

Pak mods are loaded in order defined in the (`%localappdata%\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\modsettings.lsx`) file, last loaded wins. 

`modsettings.lsx` is a XML file that contains a sorted list of pak mods and their metadata.

Pak mods can list versioned dependencies in their `meta.lsx` file, that the game will try to load before the mod itself.
Further details on the `.pak` and `meta.lsx` formats are provided below.

The game data is itself stored in pak archives, but these are located in the `Game/Data` folder. 

### Native Mods
There are a handful of popular native dlls mods, which are installed in the `Bin/NativeMods` folder.
These require a loader, like [Native Mod Loader](https://www.nexusmods.com/baldursgate3/mods/944) to be installed to work.

## BG3 Official modding support
BG3 added official modding support through ModIo in Patch 7 (sep 2024), but majority od mods are still available on NexusMods.
BG3 offers an in-game mod manager, to download, toggle and remove mods from ModIo. There is no Load Order management support as of Patch 7.
"Third party" mods are still recognized and used by the game, but they are not officially supported.

## Load Order
BG3 uses a load order for `.pak` mods, where the last loaded mod wins in case of conflicts.
Load order is defined in the `%localappdata%\Larian Studios\Baldur's Gate 3\PlayerProfiles\Public\modsettings.lsx` file, which is a XML file that contains a sorted list of pak mods and their metadata.
The metadata is defined in the `meta.lsx` file, inside the pak archives.

The format of `modsettings.lsx` as of Patch 7 is as follows:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<save>
    <version major="4" minor="7" revision="1" build="200"/>
    <region id="ModuleSettings">
        <node id="root">
            <children>
                <node id="Mods">
                    <children>
                        <node id="ModuleShortDesc">
                            <!-- Always included entry, represents the game files -->
                            <attribute id="Folder" type="LSString" value="GustavDev"/>
                            <attribute id="MD5" type="LSString" value=""/>
                            <attribute id="Name" type="LSString" value="GustavDev"/>
                            <attribute id="PublishHandle" type="uint64" value="0"/>
                            <attribute id="UUID" type="guid" value="28ac9ce2-2aba-8cda-b3b5-6e922f71b6b8"/>
                            <attribute id="Version64" type="int64" value="36028797018963968"/>
                        </node>
                        <node id="ModuleShortDesc">
                            <!-- Improved UI mod  -->
                            <attribute id="Folder" type="LSString" value="ImpUI_26922ba9-6018-5252-075d-7ff2ba6ed879"/>
                            <attribute id="MD5" type="LSString" value="0f136f38f83bb9083fedcfb4a7b8510b"/>
                            <attribute id="Name" type="LSString" value="ImpUI (ImprovedUI)"/>
                            <attribute id="PublishHandle" type="uint64" value="0"/>
                            <attribute id="UUID" type="guid" value="26922ba9-6018-5252-075d-7ff2ba6ed879"/>
                            <attribute id="Version64" type="int64" value="1"/>
                        </node>
                        <node id="ModuleShortDesc">
                            <!-- Mod Configuration Menu Mod -->
                            <attribute id="Folder" type="LSString" value="BG3MCM"/>
                            <!-- MD5 can often be empty, use UUID as identifier -->
                            <attribute id="MD5" type="LSString" value=""/>
                            <attribute id="Name" type="LSString" value="Mod Configuration Menu"/>
                            <attribute id="PublishHandle" type="uint64" value="0"/>
                            <attribute id="UUID" type="guid" value="755a8a72-407f-4f0d-9a33-274ac0f0b53d"/>
                            <attribute id="Version64" type="int64" value="38280596832649216"/>
                        </node>
                        <!-- More mods... -->
                    </children>
                </node>
            </children>
        </node>
    </region>
</save>
```

Pak archives that are present in the Mods folder, but not listed in the `modsettings.lsx` file are considered disabled by the game.
Some disabled pak files that contain overrides of files contained in vanilla pak files may still override those files, 
causing potential unexpected behavior.

Advice from mod authors is to avoid disabling pak files by excluding them from the `modsettings.lsx` file, and prefer to physically remove them from the Mods folder instead.

## Pak File Format
The `.pak` file is a file format used by Baldur's Gate 3 for distribution of mods. The file format is primarily a LZ4-compressed list of files. The data of the actual files are also compressed using LZ4. Here is a work-in-progress breakdown of the binary file format.

Version 18 is the latest version and the app supports v15 and greater.

### Header

The header contains the following fields:

If version = 15, header is 38 bytes

| Name           | Type     | Description                             |
|----------------|----------|-----------------------------------------|
| magic          | `char[4]` | Magic bytes `LSPK`                      |
| version        | `uint`   | Version number                          |
| fileListOffset | `uint64` | Position where the list of files begins |
| fileListSize   | `uint`   | Size of fileList data section           |  
| flags          | `byte`   | Flags                                   |
| priority       | `byte`   | Priority                                |
| md5            | `char[16]` | MD5 hash of ...                         |

If version >= 16, header is 40 bytes and has an additional field:

| Name           | Type     | Description |
|----------------|----------|----------|
| numParts       | `ushort` |          |

### File List

The file list is a list of files that are contained within the `.pak` file. The file list is an array of `FileEntry` structs, starts at `fileListOffset`, is `fileListSize` bytes long and is compressed using LZ4.

### FileEntry

#### Version 15 - 296 bytes

| Name             | Type     | Description         |
|------------------|----------|---------------------|
| Name             | `string` | Fixed size of `256` |
| OffsetInFile     | `ulong`  |                     |
| SizeOnDisk       | `ulong`  |                     |
| UncompressedSize | `ulong`  |                     |
| ArchivePart      | `uint`   |                     |
| Flags            | `uint`   |                     |
| Crc              | `uint`   |                     |
| Unknown2         | `uint`   |                     |

#### Version 16
File list unchanged from version 15.

#### Version 18 - 272 bytes

| Name             | Type     | Description         |
|------------------|----------|---------------------|
| Name             | `string` | Fixed size of `256` |
| OffsetInFile1    | `uint`   |                     |
| OffsetInFile2    | `ushort` |                     |
| ArchivePart      | `byte`   |                     |
| Flags            | `byte`   |                     |
| SizeOnDisk       | `uint`   |                     |
| UncompressedSize | `uint`   |                     |

### File Data
Each file that is stored within the `pak` file can be extracted using the above FileEntry information. The data for each file starts at it's `OffsetInFile` and is `SizeOnDisk` bytes long.

## Pak Metadata (`meta.lsx`)
Example of a `meta.lsx` file:
```xml
<?xml version="1.0" encoding="utf-8"?>
<save>
  <version major="4" minor="0" revision="0" build="49" />
  <region id="Config">
    <node id="root">
      <children>
        <node id="Dependencies">
          <children>
            <node id="ModuleShortDesc">
              <attribute id="Folder" type="LSWString" value="VolitionCabinet" />
              <attribute id="MD5" type="LSString" value="" />
              <attribute id="Name" type="FixedString" value="VolitionCabinet" />
              <attribute id="UUID" type="FixedString" value="f97b43be-7398-4ea5-8fe2-be7eb3d4b5ca" />
              <attribute id="Version64" type="int64" value="36028799166447616" />
            </node>
            <node id="ModuleShortDesc">
              <attribute id="Folder" type="LSWString" value="BG3MCM" />
              <attribute id="MD5" type="LSString" value="" />
              <attribute id="Name" type="FixedString" value="Mod Configuration Menu" />
              <attribute id="UUID" type="FixedString" value="755a8a72-407f-4f0d-9a33-274ac0f0b53d" />
              <attribute id="Version64" type="int64" value="36028797018963968" />
            </node>
          </children>
        </node>
        <node id="ModuleInfo">
          <attribute id="Author" type="LSString" value="Volitio" />
          <attribute id="CharacterCreationLevelName" type="FixedString" value="" />
          <attribute id="Description" type="LSString" value="Adds a new Emerald Grove waypoint inside the Grove. You can pick three different options via MCM." />
          <attribute id="Folder" type="LSString" value="WaypointInsideEmeraldGrove" />
          <attribute id="LobbyLevelName" type="FixedString" value="" />
          <attribute id="MD5" type="LSString" value="" />
          <attribute id="MainMenuBackgroundVideo" type="FixedString" value="" />
          <attribute id="MenuLevelName" type="FixedString" value="" />
          <attribute id="Name" type="FixedString" value="Waypoint Inside Emerald Grove" />
          <attribute id="NumPlayers" type="uint8" value="4" />
          <attribute id="PhotoBooth" type="FixedString" value="" />
          <attribute id="StartupLevelName" type="FixedString" value="" />
          <attribute id="Tags" type="LSString" value="" />
          <attribute id="Type" type="FixedString" value="Add-on" />
          <attribute id="UUID" type="FixedString" value="e342ee75-f7c9-4aeb-b6de-403991578337" />
          <attribute id="Version64" type="int64" value="72057598332895232" />
          <children>
            <node id="PublishVersion">
              <attribute id="Version64" type="int64" value="72057598332895232" />
            </node>
            <node id="Scripts" />
            <node id="TargetModes">
              <children>
                <node id="Target">
                  <attribute id="Object" type="FixedString" value="Story" />
                </node>
              </children>
            </node>
          </children>
        </node>
      </children>
    </node>
  </region>
</save>
```

The relevant data is:
- `Dependencies` - List of dependencies for the mod, useful for missing or outdated dependency Health Checks.
And:
- `Name`
- `Folder`
- `UUID`
- `Version64`
- `MD5`
for populating the `modsettings.lsx` file. 

A pak mod may list vanilla pak files as dependencies, which should be ignored for the purposes of health checks.
Since there is no evident way to distinguish between vanilla and mod pak files, an exclusion list of vanilla pak files is required.


## Essential Mods & Tools
- BG3SE
Requirement for a lot of mods, but not allowed for Modio mods. 
New scripting capabilities (osiris scripting) added in patch 7 may reduce the need for BG3SE in the future. 

For mod authors:
- BG3 Toolkit: https://store.steampowered.com/app/2956320/Baldurs_Gate_3_Toolkit_Data/

## Communities
- [Larian Studios Discord](https://discord.gg/larianstudios)
- [BG3 Modding Discord](https://discord.gg/bg3mods)
- [Down by the river Discord](https://discord.gg/downbytheriver)
- 
## Useful links
- [BG3 Modding Community Wiki](https://wiki.bg3.community/)

