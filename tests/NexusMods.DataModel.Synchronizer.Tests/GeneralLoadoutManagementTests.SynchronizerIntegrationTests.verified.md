## 1 - Initial State:
The initial state of the game folder should contain the game files as they were created by the game store. No loadout has been created yet.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |



## 2 - Loadout Created (A) - Synced:
A new loadout has been created and has been synchronized, so the 'Last Synced State' should be set to match the new loadout.
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



## 4 - New File Added to Game Folder:
New files have been added to the game folder by the user or the game, but the loadout hasn't been synchronized yet.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 5 - Loadout Synced with New File:
After the loadout has been synchronized, the new file should be added to the loadout.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 6 - New Loadout (B) Created - No Sync:
A new loadout is created, but it has not been synchronized yet. So again the 'Last Synced State' is not set.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 7 - New Loadout (B) Synced:
After the new loadout has been synchronized, the 'Last Synced State' should match the 'Current State' as the loadout has been applied to the game folder. Note that the contents of this 
loadout are different from the previous loadout due to the new file only being in the previous loadout.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Current State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 8 - New File Added to Game Folder (B):
A new file has been added to the game folder and B loadout has been synchronized. The new file should be added to the B loadout.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderB.txt} | 0xC6B738DF31EA91BB | 28 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderB.txt} | 0xC6B738DF31EA91BB | 28 B |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |
### Loadout B - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderB.txt} | 0xC6B738DF31EA91BB | 28 B |   |   |



## 9 - Switch back to Loadout A:
Now we switch back to the A loadout, and the new file should be removed from the game folder.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |
### Loadout B - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderB.txt} | 0xC6B738DF31EA91BB | 28 B |   |   |



## 10 - Loadout A Copied to Loadout C:
Loadout A has been copied to Loadout C, and the contents should match.
### Initial State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Last Synced State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Current State - (2)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |
### Loadout A - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |
### Loadout B - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderB.txt} | 0xC6B738DF31EA91BB | 28 B |   |   |
### Loadout C - (2)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
| {Game, bin/originalGameFile.txt} | 0x673E3C493921A2D5 | 12 B |   |   |



## 11 - Game Unmanaged:
The loadouts have been deleted and the game folder should be back to its initial state.



## 11 - Game Unmanaged:
The loadouts have been deleted and the game folder should be back to its initial state.
### Current State - (0)
| Path | Hash | Size |
| --- | --- | --- |
