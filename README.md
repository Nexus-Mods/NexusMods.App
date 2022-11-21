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


