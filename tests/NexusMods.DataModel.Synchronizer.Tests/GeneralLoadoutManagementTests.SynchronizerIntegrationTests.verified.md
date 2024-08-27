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



## 2 - Loadout Created (A) - Synced:
A new loadout has been created and has been synchronized, so the 'Last Synced State' should be set to match the new loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:100000000000008
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000008
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 4 - New File Added to Game Folder:
New files have been added to the game folder by the user or the game, but the loadout hasn't been synchronized yet.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:100000000000008
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:100000000000009
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000009 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 5 - Loadout Synced with New File:
After the loadout has been synchronized, the new file should be added to the loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:10000000000000B
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:10000000000000B
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 6 - Loadout Deactivated:
At this point the loadout is deactivated, and all the files in the current state should match the initial state.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:10000000000000D
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 7 - New Loadout (B) Created - No Sync:
A new loadout is created, but it has not been synchronized yet. So again the 'Last Synced State' is not set.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:10000000000000D
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
### Loadout B - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:20000000000000C, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:10000000000000E |



## 8 - New Loadout (B) Synced:
After the new loadout has been synchronized, the 'Last Synced State' should match the 'Current State' as the loadout has been applied to the game folder. Note that the contents of this 
loadout are different from the previous loadout due to the new file only being in the previous loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (1) - Tx:10000000000000F
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:10000000000000F
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
### Loadout B - (1)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:20000000000000C, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:10000000000000E |



## 9 - New File Added to Game Folder (B):
A new file has been added to the game folder and B loadout has been synchronized. The new file should be added to the B loadout.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:100000000000012
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000012 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:100000000000012
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000012 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
### Loadout B - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:20000000000000C, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000012 |
| (EId:20000000000000C, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:10000000000000E |



## 10 - Switch back to Loadout A:
Now we switch back to the A loadout, and the new file should be removed from the game folder.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:100000000000015
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000015 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:100000000000015
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000015 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
### Loadout B - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:20000000000000C, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000012 |
| (EId:20000000000000C, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:10000000000000E |



## 11 - Loadout A Copied to Loadout C:
Loadout A has been copied to Loadout C, and the contents should match.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (2) - Tx:100000000000015
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000015 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (2) - Tx:100000000000015
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000015 |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:10000000000000B |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |
### Loadout B - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:20000000000000C, Game, bin/newFileInGameFolderB.txt) | 0x3E6AD5D9F57F8D4E | 28 B | Tx:100000000000012 |
| (EId:20000000000000C, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:10000000000000E |
### Loadout C - (2)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000013, Game, bin/newFileInGameFolderA.txt) | 0x2D489D43D46C8849 | 25 B | Tx:100000000000016 |
| (EId:200000000000013, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000016 |



## 12 - Game Unmanaged:
The loadouts have been deleted and the game folder should be back to its initial state.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000018
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |



