## 1 - Initial State:
The initial state of the game, no loadout has been created yet.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |



## 2 - Loadout Created (A) - Synced:
Added a new loadout and synced it.
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



## 3 - Added ModA to Loadout(A) - Synced:
Added ModA to Loadout A and synced it.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (4) - Tx:10000000000000B
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (4) - Tx:10000000000000B
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (4)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 4 - Added ModB to Loadout(A) - Synced:
Added ModB to Loadout A and synced it.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (6) - Tx:10000000000000E
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modB/meshB.txt) | 0xAD77F2959C19BC3F | 23 B | Tx:10000000000000E |
| (EId:200000000000001, Game, bin/mods/modB/textureB.txt) | 0x53348C857628B664 | 26 B | Tx:10000000000000E |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (6) - Tx:10000000000000E
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modB/meshB.txt) | 0xAD77F2959C19BC3F | 23 B | Tx:10000000000000E |
| (EId:200000000000001, Game, bin/mods/modB/textureB.txt) | 0x53348C857628B664 | 26 B | Tx:10000000000000E |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (7)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/modB/meshB.txt) | 0xAD77F2959C19BC3F | 23 B | Tx:10000000000000D |
| (EId:200000000000005, Game, bin/mods/modB/textureB.txt) | 0x53348C857628B664 | 26 B | Tx:10000000000000D |
| (EId:200000000000005, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000D |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



## 5 - Disabled ModB in Loadout(A) - Synced:
Disabled ModB in Loadout A and synced it. All the ModB files should have been removed from the disk state, except for the shared file.
Files from ModA should still be present.
### Initial State - (1) - Tx:100000000000005
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Last Synced State - (4) - Tx:100000000000010
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Current State - (4) - Tx:100000000000010
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000001, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000B |
| (EId:200000000000001, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000005 |
### Loadout A - (4)
| Path | Hash | Size | TxId |
| --- | --- | --- | --- |
| (EId:200000000000005, Game, bin/mods/modA/meshA.txt) | 0xB6BD4837EFFAB020 | 23 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/modA/textureA.txt) | 0x3D6DD4C6F64436E6 | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/mods/shared/shared.txt) | 0xB8EE9F708ECD24BC | 26 B | Tx:10000000000000A |
| (EId:200000000000005, Game, bin/originalGameFile.txt) | 0xA52B286A3E7F4D91 | 12 B | Tx:100000000000007 |



