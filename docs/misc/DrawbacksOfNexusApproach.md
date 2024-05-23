!!! warning "The Nexus Mods App approach requires more book-keeping than traditional approaches"

- Keeping backup of original game files.
- Watching for changes in game folder. (Backup user/game generated files)

This takes considerable CPU and Disk resources.

## Disk Usage

!!! info "To support undo, any game files managed by the app are backed up."

This typically results in an increase in disk usage of 60-100% compared to regular game size.

!!! tip "For a 10 GB game, an additional 6-10 GB of space will be used once 'managed' with the Nexus Mods App."

For desktops, storage today is fairly cheap, however, there are still limitations.

Handheld users, such as those using the [Steam Deck][steam-deck], may end up limited in storage space
and require buying expandable storage (microSD). To accommodate such a use case, we'll need to support storing mods in
multiple locations.

## Time to First Play

!!! info "The backup process for large games may take a long time."

A 100 GB game may take around 10 minutes to backup under ideal circumstances on a system using a SATA SSD (we consider this the 'baseline').

More realistically, probably in the realm of 12-13 minutes, depending on:

- Drive activity
- CPU performance (and [Certain kind of software][microsoft-defender-antivirus])

This may be problematic if the user wants to immediately play their game, as they'll be locked waiting.

## Footnote

!!! nexus "The Nexus Mods App is built with the hardware of today in mind. Taking advantage of fast CPUs and SSDs."

With sufficient optimization and planning, we hope these processes become mostly 'transparent' to end users,
so they don't really notice a difference.

If we can be done with our work by the time an end user finds and downloads a mod they want to play,
we're probably in a good place.

With certain game stores, it may be possible to fetch the original
files from the store rather than backing them up on the user's machine. We'll have to see in the future.

[steam-deck]: https://store.steampowered.com/steamdeck
[microsoft-defender-antivirus]: https://en.wikipedia.org/wiki/Microsoft_Defender_Antivirus
