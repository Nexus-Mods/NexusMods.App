!!! note "The Nexus App is a Work in Progress"

    This documentation currently only contains information for programmers, contributors, and curious people alike.

----------

<div align="center">
	<h1>The Nexus Mods App</h1>
	<img src="./Nexus/Images/Nexus-Icon.png" width="150" align="center" />
	<br/> <br/>
    Mod With Confidence
    <br/>
    The <i>future</i> of modding with <i>Nexus</i>.
    <br/><br/>
    <a href="https://github.com/Nexus-Mods/NexusMods.App/actions/workflows/clean_environment_tests.yaml" target="_blank">
        <img src="https://github.com/Nexus-Mods/NexusMods.App/actions/workflows/clean_environment_tests.yaml/badge.svg" alt="CI Tests">
    </a>
    <a href="https://discord.gg/ReWTxb93jS" target="_blank">
        <img src="https://img.shields.io/discord/1134149061080002713?logo=discord&logoColor=white&color=7289da" alt="Discord">
    </a>
</div>

Nexus Mods App is a mod installer, creator and manager for all your popular games.

Easy to use, runs on your standard Windows PC and Linux alike. Don't waste time troubleshooting, play your games,
fill those knees with arrows and most importantly, ***Have Fun***!

!!! info "The Nexus App is about creating that Modding experience that *'Just Works'* for old and new games alike!"

### Mod with Confidence

One of the biggest complaints of users over the years about modding is that they can't mod with confidence.

- Will a new mod wreck my install?
- Will I need to delete my game to revert a mod install?
- The game updated, my mods updated, and now everything's broken.

Every step of the ***classic modding approach*** is fraught with pitfalls and destructive changes.

The Nexus Mods App intends to solve these problems. In addition to being a great mod installer, manager and builder,
this project aims to always provide an *undo* feature for users. Not just on a metadata level (like most mod managers
offer today) but on a per-file basis as well.

Go ahead, update that mod, if you don't like it, you can always go back to the game as it was before you made the update.

Concept: [Immutable Mod Lists][immutable-mod-lists]

### Simple Data

Most *Mod Managers* have serious drawbacks and edge cases in how they manage files (data).

They start with a goal of keeping the game folder clean, through the use of techniques such as Symbolic Links,
Hard Links and VFS Systems.

!!! warning "Unfortunately, most modding frameworks aren't designed with these systems in mind."

As a result, concepts foreign to end users (VFS, Symlinks) are forced upon them, and the [incidental complexity][incidental-complexity]
of these systems often leak through the mod manager abstractions in unexpected ways.

#### For Example

Symlinks & Hardlinks:

* How do you know if your game works correctly with Symlinks?
* For Hardlinks, your mod files *must be on the same drive* as your game.
* Links can have strange side-effects when files are modified, depending if the file was direct modified or deleted-created-modified.

Virtual FileSystems:

* You have to run xEdit through [Mod Organizer 2][mod-organizer-2] for it to see your mod list (Bethesda Games)
* Files end up in a different place from where the end user expects them.

#### Nexus App Approach

!!! nexus "The Nexus Mods App aims to merge the mental simplicity of manual modding, with the hygiene of existing mod installers, and a promise of: "you can always go back to what last worked""

The 'Data Model' of the Nexus Mods App can be thought of as an extension of lessons learned from the development of
Nexus Collections and Wabbajack.

When you make a mod list, we don't directly manipulate files. Instead, we manipulate the 'instructions' used to deploy
the mods to the folder, and clicking 'Apply' simply moves the files directly to the folder.

We do this in a way that allows the user to revert changes at any time. Files aren't 'linked' or 'staged' in any way,
no functions are hooked and the the [cognitive overhead][cognitive-overhead] of the modding process is greatly reduced.

Instead, users can focus on creating a perfect modding setup that works for them.

Further Reading: [Comparison of File Management Systems][comparison-fms]

#### A Drawback

!!! warning "The Nexus App approach requires more book-keeping than traditional approaches"

- Keeping backup of original game files.
- Watching for changes in game folder. (Backup user/game generated files)

This takes considerable CPU and Disk resources. To support undo, any game files managed by the app are effectively cloned,
doubling disk usage. The user may also need to wait a few minutes before being able to launch big games.

However, with sufficient optimization and planning, we hope these processes become 'transparent' to end users,
so they don't really notice a difference.

For example, with certain game stores it may be possible to fetch the original
files from the store rather than backing them up on the user's machine.

!!! info "The Nexus Mods App is built in mind with the hardware of today. Taking advantage of fast CPUs and SSDs."

## The Development Team

!!! nexus "The Nexus App is developed by a very diverse, talented group of people."

- Tim ([Halgari][halgari]): Team lead and author of [Wabbajack][wabbajack], 20 years building large computer systems, and most notably a big fan of [Clojure][clojure].

- [Sewer56][sewer56]: Best known for [Reloaded-II][reloaded-ii]. Built several modding frameworks and brings with him a lot of experience with micro-optimization, reverse engineering, and low-level knowledge; including the more arcane parts of .NET.

- [erri120][erri120]: Worked with Tim for some time on [Wabbajack][wabbajack], has been heavily involved with [Mutagen][mutagen] (patching library for Bethesda games) and wrote the [Gamefinder][gamefinder] library used in the App.

- [AL12][al12]: Best known for being lead developer of [Mod Organizer 2][mod-organizer-2]. Wealth of experience in various modding frameworks is appreciated.

We also collaborate with the [Vortex][vortex] people:

- Simon ([Simon][simon]): Been a Unity developer for some time and has quickly picked up the ins and outs of Vortex. He’s leads development on [Vortex][vortex].

- [Nagev][nagev]: Played a pivotal role in developing several impressive extensions and frameworks for the application.

Former maintainers:

- [Tannin][tannin]: Created [Vortex][vortex], and was the lead developer throughout most of its lifetime.

!!! tip "Development of the Nexus App is funded through your subscriptions of [nexus-premium][nexus-premium]"

If you enjoy using the App, consider subscribing, thanks!

[al12]: https://github.com/Al12rs
[clojure]: https://clojure.org/
[cognitive-overhead]: https://techcrunch.com/2013/04/20/cognitive-overhead/
[comparison-fms]: misc/ComparisonOfFileManagementSystems.md
[erri120]: https://github.com/erri120
[gamefinder]: https://github.com/erri120/GameFinder
[halgari]: https://github.com/halgari
[immutable-mod-lists]: concepts/0000-immutable-modlists.md
[incidental-complexity]: https://dev.to/alexbunardzic/software-complexity-essential-accidental-and-incidental-3i4d
[mod-organizer-2]: https://www.modorganizer.org/
[mutagen]: https://mutagen-modding.github.io/Mutagen/
[nagev]: https://github.com/IDCs
[nexus-premium]: https://next.nexusmods.com/premium
[reloaded-ii]: https://reloaded-project.github.io/Reloaded-II/
[sewer56]: https://github.com/Sewer56
[simon]: https://github.com/insomnious
[tannin]: https://github.com/TanninOne
[vortex]: https://www.nexusmods.com/about/vortex/
[wabbajack]: https://www.wabbajack.org/
