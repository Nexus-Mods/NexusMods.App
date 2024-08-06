## 1 - Initial State:
The initial state of the game folder should contain the game files as they were created by the game store. No loadout has been created yet.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |



## 2 - Loadout Created (A) - No Sync:
A new loadout has been created, but it hasn't been synchronized yet, so the 'Last Synced State' isn't set to a value even though the loadout contains the initial
game files.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 3 - Loadout Created - Synced:
Now that the loadout has been synchronized, the 'Last Synced State' should match the 'Current State' as the loadout has been applied to the game folder.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:100000000000009
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000009
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 4 - New File Added to Game Folder:
New files have been added to the game folder by the user or the game, but the loadout hasn't been synchronized yet.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:100000000000009
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:10000000000000A
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000A |
### Loadout A - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 5 - Loadout Synced with New File:
After the loadout has been synchronized, the new file should be added to the loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:10000000000000C
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Current State - (2) - Tx:10000000000000C
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |



## 6 - Loadout Deactivated:
At this point the loadout is deactivated, and all the files in the current state should match the initial state.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:10000000000000E
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |



## 7 - New Loadout (B) Created - No Sync:
A new loadout is created, but it has not been synchronized yet. So again the 'Last Synced State' is not set.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:10000000000000E
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Loadout B - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000010 |



## 8 - New Loadout (B) Synced:
After the new loadout has been synchronized, the 'Last Synced State' should match the 'Current State' as the loadout has been applied to the game folder. Note that the contents of this 
loadout are different from the previous loadout due to the new file only being in the previous loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:100000000000011
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000011
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Loadout B - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000010 |



## 9 - New File Added to Game Folder (B):
A new file has been added to the game folder and B loadout has been synchronized. The new file should be added to the B loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:100000000000014
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000014 |
### Current State - (2) - Tx:100000000000014
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000014 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Loadout B - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000010 |
| (EId:20000000000000D, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000014 |



## 10 - Switch back to Loadout A:
Now we switch back to the A loadout, and the new file should be removed from the game folder.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:100000000000017
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000017 |
### Current State - (2) - Tx:100000000000017
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000017 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000C |
### Loadout B - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:300020000000002, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000010 |
| (EId:20000000000000D, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000014 |



## 11 - Game Unmanaged:
The loadouts have been deleted and the game folder should be back to its initial state.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000019
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |



