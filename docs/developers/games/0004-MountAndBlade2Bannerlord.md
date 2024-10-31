## General Info

- Name: Mount & Blade II: Bannerlord
- Release Date: 2020
- Engine: Custom - C++ Foundation, C# Scripting

### Stores and Ids

- [Steam](https://store.steampowered.com/app/261550/Mount__Blade_II_Bannerlord/): `261550`
- [GOG](https://www.gog.com/game/mount_blade_ii_bannerlord): `1802539526`, `1564781494`
- [Epic Game Store](https://store.epicgames.com/en-US/p/mount-and-blade-2): `Chickadee`
- [Xbox Game Pass](https://www.xbox.com/en-US/games/store/mount-blade-ii-bannerlord/9pdhwz7x3p03): `TaleWorldsEntertainment.MountBladeIIBannerlord`

### Useful Links

- [Official Mod Docs](https://moddocs.bannerlord.com)
- [Community Mod Docs](https://docs.bannerlordmodding.com)

### Engine and Mod Support

Bannerlord uses a native engine with self-hosted .NET 6 for Steam/GOG/Epic.
Modding is supported out of the box.

Bannerlord has a modding extension [BLSE](https://www.nexusmods.com/mountandblade2bannerlord/mods/1)
that expands the modding capabilities.

It's required to run mods on Xbox and is optional for Steam/GOG/Epic.

## Installing Mods (Outside of NMA)

### Automatic

- [Steam Workshop](https://steamcommunity.com/app/261550/workshop/)

Typically installs immediately via Steam Client

### Manual

- [Nexus](https://www.nexusmods.com/mountandblade2bannerlord)
- [ModDB](https://www.moddb.com/games/mount-blade-ii-bannerlord/mods)

## Mod Types

- [BLSE](https://www.nexusmods.com/mountandblade2bannerlord/mods/1)
- [Module (Standard) Mods](#typical-mod-structure-module).

Since it's not a standard mod.

### BLSE

BLSE needs to be placed beside the main game binary:

- `bin/Gaming.Desktop.x64_Shipping_Client` for Xbox Store
- `bin/Win64_Shipping_Client` for other targets

Outside of that, we will want to launch BLSE rather than the regular game binary if BLSE is installed.

### Typical Mod Structure (Module)

[See: Bannerlord Documentation](https://docs.bannerlordmodding.com/_intro/folder-structure.html#folder-descriptions-file-examples)

```
MyModule
├── AssetPackages
│   └── assetpackage.tpac
├── Atmospheres
│   ├── Interpolated
│   │   └── interpolatedatmosphere.xml
│   └── atmosphere.xml
├── bin
│   └── Win64_Shipping_Client
│       └── MyModule.dll
├── GUI
│   ├── Brushes
│   └── Prefabs
├── ModuleData
├── SceneObj
└── SubModule.xml
```

The only needed file is `SubModule.xml`, this is the metadata file.
All else is optional.

Note that the `bin` folder contains DLLs loaded by the game, don't mistake this tree with
the folder structure of the game itself.

#### Example `SubModule.xml`

Taken from [Player Settlements](https://www.nexusmods.com/mountandblade2bannerlord/mods/7298?tab=files&file_id=44128)

```xml
<Module>
  <Name value="Player Settlement"/>
  <Id value="PlayerSettlement"/>
  <Version value="v6.0.2"/>
  <Url value="https://www.nexusmods.com/mountandblade2bannerlord/mods/7298" />
  <SingleplayerModule value="true"/>
  <MultiplayerModule value="false"/>
  <Official value="false"/>
  <DefaultModule value="false" />
  <ModuleCategory value="Singleplayer" />
  <ModuleType value="Community" />
  <UpdateInfo value="NexusMods:7298" />
  <!-- Used by PlayerSettlement to dynamically add more settlement variants -->
  <PlayerSettlementsTemplates path="ModuleData/Player_Settlement_Templates" />
  <!-- Used by PlayerSettlement to dynamically blacklist settlement variants -->
  <PlayerSettlementsTemplatesBlacklist path="ModuleData/template_blacklist.txt" />
  <DependedModules>
    <DependedModule Id="Bannerlord.Harmony" />
    <DependedModule Id="Bannerlord.ButterLib" />
    <DependedModule Id="Bannerlord.UIExtenderEx" />
    <DependedModule Id="Bannerlord.MBOptionScreen" />
    <DependedModule Id="Native"/>
    <DependedModule Id="SandBoxCore"/>
    <DependedModule Id="Sandbox"/>
    <DependedModule Id="StoryMode" />
  </DependedModules>
  <!-- Community Metadata -->
  <DependedModuleMetadatas>
    <DependedModuleMetadata id="Bannerlord.Harmony" order="LoadBeforeThis" version="v2.2.2" />
    <DependedModuleMetadata id="Bannerlord.ButterLib" order="LoadBeforeThis" version="v2.8.15" />
    <DependedModuleMetadata id="Bannerlord.UIExtenderEx" order="LoadBeforeThis" version="v2.12.0" />
    <DependedModuleMetadata id="Bannerlord.MBOptionScreen" order="LoadBeforeThis" version="v5.10.1" />
    <DependedModuleMetadata id="Native" order="LoadBeforeThis" version="1.0.0.*" />
    <DependedModuleMetadata id="SandBoxCore" order="LoadBeforeThis" version="1.0.0.*" />
    <DependedModuleMetadata id="Sandbox" order="LoadBeforeThis" version="1.0.0.*" />
    <DependedModuleMetadata id="StoryMode" order="LoadBeforeThis" version="1.0.0.*" />
    <DependedModuleMetadata id="CustomBattle" order="LoadBeforeThis" version="1.0.0.*" optional="true" />
  </DependedModuleMetadatas>
  <SubModules>
    <SubModule>
      <Name value="PlayerSettlement"/>
      <DLLName value="PlayerSettlement.dll"/>
      <SubModuleClassType value="BannerlordPlayerSettlement.Main"/>
      <Tags />
    </SubModule>
  </SubModules>
  <Xmls/>
</Module>
```

#### Per-Store Mods

It's possible to have per-store specific code for Bannerlord, namely:

```
📁 Gaming.Desktop.x64_Shipping_Client
    📄 0Harmony.dll (2.2 MB)
    📄 MCMv5.dll (493.1 kB)
    📄 PlayerSettlement.dll (250.9 kB)
    📄 PlayerSettlement.pdb (97.1 kB)
    📄 System.Numerics.Vectors.dll (115.9 kB)
📁 Win64_Shipping_Client
    📄 0Harmony.dll (2.2 MB)
    📄 MCMv5.dll (493.1 kB)
    📄 PlayerSettlement.dll (250.9 kB)
    📄 PlayerSettlement.pdb (97.1 kB)
    📄 System.Numerics.Vectors.dll (115.9 kB)
```

- `Gaming.Desktop.x64_Shipping_Client` applies to Xbox Store
- `Win64_Shipping_Client` applies to other targets

#### Mod Load Order

As seen in `order="LoadBeforeThis`, some mods may be rearranged on boot in terms of load order.
This needs to be expressed in App UI.

#### Mod Names

We should ideally extract the mod name from the `SubModule.xml` if possible,
i.e. from `<Name value="Player Settlement"/>`.

If there are multiple mods in 1 download, we may need to combine it with the mod page name.

## Outside Changes

The game has a built-in mod manager.

If the user chooses to rearrange mods in the 1st party launcher, we will need to ingest 
the changes. Ask team for clarification given last week's discussion.

### Ingesting RuntimeDataCache Folders

Mod modules can generate a `RuntimeDataCache` folder their module at runtime; which contains
assets ready for consumption generated by the game engine (from the module sources).

Computing this folder's contents is a costly operation, for large total conversion mods, this can
even take half an hour, or longer on some machines based on reports.

It's imperative we ingest the files generated in this folder right into the App, so the user can
quickly switch between loadouts. An override of `MoveNewFilesToMods` should do the trick.

## Diagnostics

[Diagnostics NMA Wiki Link](https://nexus-mods.github.io/NexusMods.App/developers/development-guidelines/Diagnostics/#choosing-the-severity)

### Warning: Gauntlet (non-C#) Modules Must load Before Official Module

[Modding Wiki Link](https://docs.bannerlordmodding.com/_tutorials/modding-gauntlet-without-csharp.html#important)

Severity: `Warning`
Summary: `Codeless Gauntlet UI Mods must be loaded before Official Modules.`
Details: Mods which override Gauntlet UIs from Official Modules MUST be loaded before said module.

To detect this, look at mods which override the `GUI/Prefabs` folder. There may be more folders.

### Suggestion: Mods Should load after Official Modules

[Modding Wiki Link](https://docs.bannerlordmodding.com/_tutorials/modding-gauntlet-without-csharp.html#important)

Severity: `Suggestion`
Summary: `Mods should load after official modules.`
Details: User created mods should load after the following official modules. `Native`, `SandBox`, `Sandbox Core`, `CustomBattle`, `StoryMode`.

Note: [Excludes Gauntlet non-C# Modules](#warning-gauntlet-non-c-modules-must-load-before-official-module).

### Critical: Missing Dependency Submodules

[Relevant Documentation](https://docs.bannerlordmodding.com/_xmldocs/submodule.html#element-descriptions)

Severity: `Critical`
Summary: `Missing Required Module {ModuleA} required by {ModuleB}`
Details: `{ModuleB}` requires `{ModuleA}` but is not present. You must download `{ModuleA}` first.

We check against the `DependedModules` array of `SubModule.xml`. 
Question: Is there a database of Module IDs to download pages?

### Warning: Duplicated Module ID Detected

[Relevant Documentation](https://docs.bannerlordmodding.com/_xmldocs/submodule.html#element-descriptions)

Severity: `Warning`
Summary: `Mod conflict: {ModA} and {ModB} can't run together (using same ID).`
Details: 

```
We found two mods that can't run together:
- {ModA} (version {VersionA})
- {ModB} (version {VersionB})

They're using the same identifier ({ModId}), so you'll need to pick one and disable the other.
```

***if the mod versions match, the text should change a little bit:***

Summary: `{ModA} might be installed twice (found two copies with version {VersionA}).`
Details:

```
We found what looks like a duplicate mod:
- {ModA} (version {VersionA})
- {ModB} (version {VersionA})

Since they're using the same identifier ({ModId}) and have identical versions, you might have
accidentally installed the same mod twice. Check your loadout and remove one copy.
```

To determine this, check ID and version of `SubModule.xml`. 

## Diagnostics (Potential/Future)

### Missing Bin Files for Current Store

[Note: Doublecheck with Community if Copying Stuff across Folders is Ok]

Presumably a mod which has `Win64_Shipping_Client` folder only will not work on Xbox (Game Pass)
but will work on other platforms. Likewise a mod with only `Gaming.Desktop.x64_Shipping_Client` will
only work on Xbox.

Whether we copy `Win64_Shipping_Client` to `Gaming.Desktop.x64_Shipping_Client` and vice versa
will require discussion with rest of team and community.

## Questions (Aragas & Co)

### What's Missing in Existing Mod Managers

Some mod pages say 'Installing with some mod managers may cause a crash'.
I believe this info is out of date, but need to doublecheck.

### Auto Copy Binary Files Across Store Releases

Discuss whether we should [copy binary files across store releases](#missing-bin-files-for-current-store).

### Display Native Game Modules

Should we show the native game modules, `Native`, `SandBox`, `Sandbox Core`, `CustomBattle`, `StoryMode`. 
Doing this may require some new tech in the App.

### Diagnostics for Save Breaking Features

Are there diagnostics for things that break saves?

That's not well defined in docs outside of https://docs.bannerlordmodding.com/_intro/general-recommendations.html (not linked in sidebar).

## Questions (Future Features)

- https://docs.bannerlordmodding.com/_xmldocs/submodule.html#element-descriptions Description is unclear.
  - "XMLs with the same id from two separate mods (or the same mod) will have their assets combined and NOT overwritten." refers to `<XmlName id=`, so multiple entries from those will be combined. 
  - Does 'overwritten' work with regards to when the 'path' field is the same?
  - Should we inform the user of this conflict in load order?
  - Highlight mods with same item IDs as conflicts https://docs.bannerlordmodding.com/_xmldocs/items/item/
  
- Some mods are packed with `.tpac`. (a.k.a. 'Taleworlds Package' named by some people)
    - Does this affect load ordering?
    - Can a `.tpac` file contain any content that 'maps' onto the module folder?
    - Should we emit a diagnostic if a mod can be packed as `.tpac` for performance reasons?