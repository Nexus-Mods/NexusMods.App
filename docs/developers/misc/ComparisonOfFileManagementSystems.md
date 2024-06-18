# Comparison of File Management Systems

There are three primary methods for file management in mod managers.
This is an overview of these systems.

Called out in this doc are the ways in which the mod managers do destructive edits to the game folders.

These destructive edits are troublesome when they require the user to recover from them by going to their game launcher
and “Verifying Local Files”.

Editing the game files themselves, however is fairly rare. Most “destructive edits” exist in the form of additional
files, that interact with the game via their presence in game folders. This includes both mod files in mod folders
supported by the game, and proxy DLLs that override certain methods in the game engine (such as ENBs).


|                        | Nexus Mod Manager                   | Vortex                          | Mod Organizer 2                                | Nexus Mods App                                             |
|------------------------|-------------------------------------|---------------------------------|------------------------------------------------|------------------------------------------------------------|
| Deployment Method      | Hard/Sym Links                      | Hard/Sym Links                  | UVFS                                           | File Copy / Hashing                                        |
| File Deleted / Created | Changes are left in Game Folder     | Changes are left in Game Folder | File exists after create, original is replaced | Detected, new modlist with changes is created              |
| File Modified          | Changes Staged File                 | Changes staged file             | Changes staged file                            | Detected, new modlist with changes is created              |
| Conflict Resolution    | Last Activated Wins                 | Rule-based                      | Order based (individual files can be hidden)   | Flexible, delegated to a replacable component              |
| Changing Conflicts     | Requires re-activating mods         | Requires re-deployment          | Requires re-launching the game                 | Requires copying in the changed files                      |
| Generated Files        | Left in the game folder             | Left in the game folder         | Added to an `Overrides` mod                    | Added to a overrides mod, existing mods, or pre-tagged mod |
| Extracted Mod Files    | Stored in Staging Folder            | Stored in staging folder        | Stored in staging folder                       | Left in original downloads until required                  |
| External Apps          | See files in game folders           | See files in game folders       | Do not see files in game folders               | See Files in game folders                                  |
| SKSE/ENB/CET           | Treated like a normal mod           | Treated like a normal mod       | Requires manual installation                   | Treated like a normal mod                                  |

