## 1 - Loadout A Synced:
Loadout A has been synchronized, and the game folder should match the loadout.
### Initial State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Last Synced State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Current State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Loadout A - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 2 - New File Added to Game Folder:
A new file has been added to the game folder, and the loadout has been synchronized. The new file should be added to the loadout.
### Initial State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Last Synced State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
### Loadout A - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 3 - Loadout A Synced with New File:
Loadout A has been synchronized again, and the new file should be added to the disk state.
### Initial State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Last Synced State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
### Loadout A - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 4 - Loadout B Synced:
Loadout B has been synchronized, the added file should be removed from the disk state, and only exist in loadout A.
### Initial State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Last Synced State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Current State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Loadout A - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



## 5 - Loadout A Synced Again:
Loadout A has been synchronized again, and the new file should be added to the disk state.
### Initial State - (0)
| Path | Hash | Size |
| --- | --- | --- |
### Last Synced State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
### Current State - (1)
| Path | Hash | Size |
| --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |
### Loadout A - (1)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |
| {Game, bin/newFileInGameFolderA.txt} | 0x3FB1DBAC894B6380 | 25 B |   |   |
### Loadout B - (0)
| Path | Hash | Size | Disabled | Deleted |
| --- | --- | --- | --- | --- |



