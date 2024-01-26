# Nexus Mods App

[![CI Tests](https://github.com/Nexus-Mods/NexusMods.App/actions/workflows/clean_environment_tests.yaml/badge.svg)](https://github.com/Nexus-Mods/NexusMods.App/actions/workflows/clean_environment_tests.yaml)
[![Discord](https://img.shields.io/discord/1134149061080002713?logo=discord&logoColor=white&color=7289da)](https://discord.gg/ReWTxb93jS)


This is the repository for the Nexus Mods App, a mod installer, creator and manager for all your popular games

## Mod with confidence

One of the biggest complaints of users over the years about mod managers is that they can't mod with confidence. Will a new mod wreck their
existing install? Will copying some files into their game folder require them to delete the entire game if they want to revert these changes? The
game updated, and the user updated their mods, now nothing works! Every step of the modding process is fraught with pitfalls and destructive changes.

The Nexus Mods App intends to solve these problems. In addition to being a great mod installer, manager and builder, this project aims to always
provide an "undo" feature for users. Not just on a metadata level (like most mod managers offer today) but on a per-file basis as well. So go ahead,
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

TL;DR - The Nexus Mods App aims to merge the mental simplicity of manual modding, with the hygiene of existing mod installers, and a promise of: "you can always go back to what last worked"

Further Reading: [Comparison of File Management Systems](/docs/ComparisonOfFileManagementSystems.md)

## FAQ

**Q:** What is this? A replacement for Vortex?

**A:** Eventually, yes. But not for quite some time, at the moment this software should be considered "pre-alpha". Do not ask for support for these tools until official announcements and a product launch is made. Feel free to direct any questions to halgari in the meantime.


**Q:** Why is this being done, why a new platform, design, etc.?

**A:** Vortex was designed primarily for development by a team of one person (Tannin), as at the time Nexus wasn't ready to take on the financial and leadership burden of a larger development team. However, times change and we're now in the position of being able to have a more well structured team and development schedule. As such we are increasing the size of our mod management team. On the technical side of things we're taking time to lay a solid CI foundation, setup a well rounded datamodel (using lessons we've learned over the years of developing Vortex), and build this project to be more of a company product and less of a community project.


**Q:** Company product? Is this going closed source?

**A:** Absolutely not, modding tools should be free, and the Nexus Mods App will always be open source (GPL3). But we also want to use this app as a way to get tools into the hands of modders and users. Instead of publishing only a download API, we want to give users a download CLI tool. Once file uploading is reworked on the site, this repo will contain the code and CLI tools required for authenticating with the Nexus and uploading files via a CLI (and eventually a UI). In short, this is us getting serious about supporting (and leading) the desktop side of modding, not just the file hosting side.


**Q:** I see tests run on Linux, Windows and OSX, are you targeting all those platforms?

**A:** Yes, the CLI runs on these platforms and we run our CI on each of these OSes. What games are supported on these platforms (e.g. do we support Skyrim through Wine on Linux?) is yet to be determined.
