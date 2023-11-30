## General Info

- Name: Blade & Sorcery
- Release Date: TBD
- Early Access Release Date: 11 Dec. 2018
- Engine: Unity

### Stores and IDs

- [Steam](https://store.steampowered.com/app/629730/Blade_and_Sorcery/): `629730`

### Community

- [Blade & Sorcery Discord](https://discord.com/invite/bladeandsorcery)

## Mod support

The game developers maintain an SDK for modding through the official mod support: [Blade & Sorcery SDK on GitHub](https://github.com/KospY/BasSDK).
Along with a wiki explaining the process of installing and creating mods: [Blade & Sorcery modding wiki](https://kospy.github.io/BasSDK).

All mods should be exported using Unity as Unity Assets.

## Overview of Mod loading process(es)

Mods are loaded each into a separate folder under `BladeAndSorcery/BladeAndSorcery_Data/StreamingAssets/Mods`.

## Uploaded Files Structure

The mod archive should contain a single named folder with the exported Unity Asset files.
The contained mod folder will then be copied to the `BladeAndSorcery/BladeAndSorcery_Data/StreamingAssets/Mods` folder and show up in the in-game mod manager.

Expected file structure:
```
MyMod/
├── manifest.json
└── ... other included mod files
```
*Each Unity mod for Blade & Sorcery **requires** a `manifest.json` file, which describes the mod. Without it the mod will not be recognized and installed.*

## Additional Considerations for Manager

There is an in-game mod manager, which can also be used to install, update and sort mods. These mods should be ingested into the loadout.

## Essential Mods & Tools

N/A

## Deployment Strategy

N/A

## Work To Do

N/A

## Misc Notes

In Vortex there used to be info about "overwrite" mods, that are directly applied over the `BladeAndSorcery/BladeAndSorcery_Data/StreamingAssets/Default`, however I haven't been able to find any. I guess they phased out with the introduction of modding SDK and later on integrated mod manager, which wouldn't allow these mods anyway.
