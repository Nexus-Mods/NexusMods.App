# Nexus Mods App

This is the repository for the Nexus Mods App, a mod installer, creator and manager for all your popular games

## Mod with confidence

One of the biggest complaints of users over the years about mod managers is that they can't mod with confidence. Will a new mod wreck their
existing install? Will copying some files into their game folder require them to delete the entire game if they want to revert these changes? The
game updated, and the user updated their mods, now nothing works! Every step of the modding process is fraught with pitfalls and destructive changes.

The Nexus Mods App intends to solve these problems. In addition to being a great mod installer, manager and builder, this project aims to always
provide a "undo" feature for users. Not just on a metadata level (like most mod managers offer today) but on a per-file basis as well. So go ahead,
update that mod, if you don't like it, you can always go back to the game as it was before you made the update.

Further Reading : [Immutable Mod Lists](/docs/ImmutableModlists.md)

## Simple Data Model

Most Mod managers have serious drawbacks and edge cases in how they manage files, most of these systems (hard links, VFS systems) start with a goal
of keeping the game folder clean, but have the side effect of introducing concepts that are foreign to users. The average gamer isn't aware of concepts
such as function hooking (VFS) or hard links. In addition the [incidental complexity](https://dev.to/alexbunardzic/software-complexity-essential-accidental-and-incidental-3i4d)
of these systems often leak through the mod manager abstractions in unexpected ways.

For example:

* You have to run xEdit through MO2's app for it to see your mod list
* Your staging folders must be on the same drive as your game if your game doesn't support symlinks
* How do you know if your game supports symlinks?
* Links have strange side-effects when files are modified, depending if the file was direct modified or deleted-created-modified.

The Datamodel of the Nexus Mods App can be thought of as an extension of lessons learned from the development of Nexus Collections and Wabbajack.
Put simply, this app doesn't directly manipulate files while creating your modlist. Instead users manipulate the install instructions of the mod list
and then clicking "Apply" writes this mod list directly to the game folder, but in a way that allows users to revert these changes at any time. Since files are not staged or
linked in game folders, and no functions are hooked, the [cognitive overhead](https://techcrunch.com/2013/04/20/cognitive-overhead/) of the modding process is greatly reduced,
and users can focus on creating a perfect modding setup.

TL;DR - The Nexus Mods App aims to merge the mental simplicity of manual modding, with the hygine of existing mod installers, and a promise of: "you can always go back to what last worked"

Further Reading: [Comparison of File Management Systems](/docs/ComparisonOfFileManagementSystems.md)

## Example usage (CLI interface)

To start with the basic development loop, let's begin by Listing the available games on our system

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe list-games
┌────────────────────────┬───────────┬────────────────────────────────────────────────────────┐
│ Game                   │ Version   │ Path                                                   │
├────────────────────────┼───────────┼────────────────────────────────────────────────────────┤
│ Darkest Dungeon        │ 1.0.0.0   │ C:\Games\Steam\steamapps\common\DarkestDungeon         │
│ Skyrim Special Edition │ 1.6.640.0 │ C:\Games\Steam\steamapps\common\Skyrim Special Edition │
│ Skyrim Special Edition │ 1.6.659.0 │ C:\GOG Galaxy\Games\Skyrim Anniversary Edition         │
└────────────────────────┴───────────┴────────────────────────────────────────────────────────┘
```


We can see here that I have Darkest Dungeon and two copies of Skyrim SE installed. Let's start by creating a baseline load order:


```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe manage-game -g darkestdungeon -v 1.0.0.0 -n BaseGame
00:00:00.000 [INFO] Indexing game files
00:00:00.342 [INFO] Creating Modlist BaseGame
0:00:00.799 [INFO] Modlist BaseGame 108117E9C34ABD4F9A5F59360D55E5AC created

```


And a load order for our mods:

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe manage-game -g darkestdungeon -v 1.0.0.0 -n MainLO
00:00:00.000 [INFO] Indexing game files
00:00:00.291 [INFO] Creating Modlist MainLO
00:00:00.778 [INFO] Modlist MainLO 504F31B2958D6E488891AB5260ABAFDA created
```

From here we can list our load orders:

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe list-managed-games
┌──────────┬──────────────────────────┬──────────────────────────────────┬───────────┐
│ Name     │ Game                     │ Id                               │ Mod Count │
├──────────┼──────────────────────────┼──────────────────────────────────┼───────────┤
│ BaseGame │ Darkest Dungeon v1.0.0.0 │ 108117E9C34ABD4F9A5F59360D55E5AC │ 1         │
│ MainLO   │ Darkest Dungeon v1.0.0.0 │ 504F31B2958D6E488891AB5260ABAFDA │ 1         │
└──────────┴──────────────────────────┴──────────────────────────────────┴───────────┘
```

And install a mod:

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe install-mod -m MainLO -f "c:\tmp\mods\MarvinSeo Sister.zip" -n "MarvinSeo Sister"
```

After that we can list the mods in our load order:

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe list-mods -m MainLO
┌──────────────────┬────────────┐
│ Name             │ File Count │
├──────────────────┼────────────┤
│ Game Files       │ 12311      │
│ MarvinSeo Sister │ 202        │
└──────────────────┴────────────┘
```

At this point, no changes have been made to the game folder, to do this we first need to generate an "Apply" or install plan:

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe apply -m MainLO -s true
┌────────────┬───────┬───────────┐
│ Action     │ Count │ Size      │
├────────────┼───────┼───────────┤
│ BackupFile │ 12308 │ 3.71 GB   │
│ CopyFile   │ 202   │ 57.695 MB │
└────────────┴───────┴───────────┘
```
Based on this plan we see that we have 4GB of files to backup (in-case they are modified by the user) and 57MB of files to copy
into the game folder. So let's go ahead now and apply this plan.

```
c:\oss\NexusMods.App\bin\win-x64>NexusMods.App.exe apply -m MainLO -s true -r true
```

At this point we will see the mod files in the game folder. Not via links or function hooking but the actual files. If we try to create
a plan with the same load order, we'll see that there is no work to be done.