## 1 - Initial State:
The initial state of the game, no loadout has been created yet.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |



## 2 - Loadout Created (A) - Synced:
Added a new loadout and synced it.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 3 - Added ModA to Loadout(A) - Synced:
Added ModA to Loadout A and synced it.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (4)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (4)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (4)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |   |   |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |   |   |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 4 - Added ModB to Loadout(A) - Synced:
Added ModB to Loadout A and synced it.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (6)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/modB/meshB.txt} | 0xEDBA825443602167 | 23 B |
| {Game, bin/mods/modB/textureB.txt} | 0x2A4D644D5A59D225 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (6)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/modB/meshB.txt} | 0xEDBA825443602167 | 23 B |
| {Game, bin/mods/modB/textureB.txt} | 0x2A4D644D5A59D225 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (7)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |   |   |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |   |   |
| {Game, bin/mods/modB/meshB.txt} | 0xEDBA825443602167 | 23 B |   |   |
| {Game, bin/mods/modB/textureB.txt} | 0x2A4D644D5A59D225 | 26 B |   |   |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |   |   |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 5 - Disabled ModB in Loadout(A) - Synced:
Disabled ModB in Loadout A and synced it. All the ModB files should have been removed from the disk state, except for the shared file.
Files from ModA should still be present.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (4)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (4)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (7)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/mods/modA/meshA.txt} | 0x47209A52BBA83A91 | 23 B |   |   |
| {Game, bin/mods/modA/textureA.txt} | 0x2D2FFBBAF1C5ED90 | 26 B |   |   |
| {Game, bin/mods/modB/meshB.txt} | 0xEDBA825443602167 | 23 B | Disabled |   |
| {Game, bin/mods/modB/textureB.txt} | 0x2A4D644D5A59D225 | 26 B | Disabled |   |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B |   |   |
| {Game, bin/mods/shared/shared.txt} | 0x0E1ADF094A2D7E0A | 26 B | Disabled |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



