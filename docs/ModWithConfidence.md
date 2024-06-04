One of the biggest complaints from users over the years about modding is that they can't mod with confidence.

- Will a new mod wreck my installation?
- Will I need to delete my game to revert a mod installation?
- The game updated, my mods updated, and now everything's broken.

Every step of the ***classic modding approach*** is fraught with pitfalls and destructive changes.

The Nexus Mods App intends to solve these problems. In addition to being a great mod installer, manager and builder,
this project aims to always provide an *undo* feature for users. Not just on a metadata level (like most mod managers
offer today) but on a per-file basis as well.

Go ahead and update that mod; if you don't like it, you can always go back to the game as it was before you made the update.

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

#### Nexus Mods App Approach

!!! nexus "The Nexus Mods App aims to merge the mental simplicity of manual modding with the hygiene of existing mod installers and a promise that "you can always go back to what last worked""

The 'Data Model' of the Nexus Mods App can be thought of as an extension of lessons learned from the development of
Nexus Collections and Wabbajack.

When you make a mod list, we don't directly manipulate files. Instead, we manipulate the 'instructions' used to deploy
the mods to the folder, and clicking 'Apply' simply moves the files directly to the folder.

We do this in a way that allows the user to revert changes at any time. Files aren't 'linked' or 'staged' in any way;
no functions are hooked, and the [cognitive overhead][cognitive-overhead] of the modding process is greatly reduced.

Instead, users can focus on creating a perfect modding setup that works for them.

Further Reading: [Comparison of File Management Systems][comparison-fms]

[al12]: https://github.com/Al12rs
[clojure]: https://clojure.org/
[cognitive-overhead]: https://techcrunch.com/2013/04/20/cognitive-overhead/
[comparison-fms]: misc/ComparisonOfFileManagementSystems.md
[erri120]: https://github.com/erri120
[flaws]: misc/DrawbacksOfNexusApproach.md
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
