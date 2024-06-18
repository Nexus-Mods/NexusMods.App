```
# These are optional elements. Feel free to remove any of them.
status: {proposed}
date: {2023-11-21 when the decision was last updated}
deciders: {App Team}
```

# Default Storage Locations for NexusMods.App Files

## Context and Problem Statement

The App currently stores its files in the same folder as the application (`{EntryFolder}`).

This approach has issues with certain packaging formats. For example, when the application is run from
an AppImage, the `EntryFolder` becomes read-only, and our application fails to run.

We need to identify alternative storage locations that are both accessible and writable, regardless of how the
application is deployed or executed.

## The Current Situation

| Packaging System        | Can Write Entry Directory? | Notes                                                                       |
|-------------------------|----------------------------|-----------------------------------------------------------------------------|
| Windows (User Folder)   | ✅                          | No issues.                                                                  |
| Windows (Program Files) | ⚠️                         | Requires elevated permissions.                                              |
| macOS (User Folder)     | ✅                          | No issues.                                                                  |
| macOS (/Applications)   | ⚠️                         | Requires elevated permissions. Reportedly affects code signing workflow.    |
| Linux (AppImage)        | ⚠️                         | Read only. Can write to `.AppImage` folder via `$OWD` environment variable. |
| Linux (Flatpak)         | ❌                          | Read only.                                                                  |

## Per Distribution Method Notes

| Packaging System | Notes                                                                                                             |
|------------------|-------------------------------------------------------------------------------------------------------------------|
| Windows          | Requires elevated permissions for non-user folders.                                                               |
| macOS            | Requires elevated permissions for non-user folders.                                                               |
| Linux (AppImage) | Single file app. Mounted on launch.                                                                               |
| Linux (Flatpak)  | Can only access certain directories by default. Full FileSystem permissions may be requested via manifest change. |

## Decision Drivers

* On uninstall, the application should be able to remove all of its files from the system.
* Default storage locations should be configurable.
    * This is already the case today, but stating this here marks the functionality as a requirement.
* The application should be 'portable'.
    * In other words, it should be able to be run from a USB stick, etc.
* Application data should be user accessible.
    * In other words, easy to find/navigate to.
    * Because application data, also contains logs and user accessible files.
    * e.g. If the App crashes on boot, finding logs should be easy.
* Cross-platform consistency.
    * The paths should be consistent across different operating systems.
    * For example, if on one platform it is in entry directory, it should do the same on other platforms too.
* The user must have write access to the directory.

## Considered Options

* **Option 0: Keep using Entry Directory**
* **Option 1: User's Home Directory**
* **Option 2: Operating System's AppData Directory**

## Decision Outcome

We went with `Option 2: Operating System's AppData Directory`.

With the following caveats/notes:

- We will assume the machine is single user for now. (no `Roaming` directory)
- We will use idiomatic paths for each operating system.

| Directory       | Windows        | Linux             |
|-----------------|----------------|-------------------|
| DataModel       | %localappdata% | XDG_DATA_HOME     |
| Temporary Files | %temp%         | XDG_STATE_HOME ⚠️ |
| Logs            | %localappdata% | XDG_STATE_HOME    |

⚠️ Non-standard location. This is because we risk RAM starvation on large downloads as `tmpfs` is often in RAM+swap.

Inside `NexusMods.App` subfolder, of course.
macOS is not yet implemented, so is currently omitted.

## Pros and Cons of the Options

### Option 0: Keep using Entry Directory

* Good, because it easily makes the application portable.
* Good, because it is consistent across deployment methods.
* Good, because it makes user data very easy to find.
* Bad, because it is incompatible with many packaging systems.

### Option 1: User's Home Directory

In a subfolder of:

- `~` on Linux/macOS
- `C:\Users\{User}` on Windows

* Good, because it is easy to find.
* Good, because it remains consistent across different deployment methods.
* Bad, because it can clutter the user's personal space with application data. ('yet another folder in my home
  directory')

### Option 2: Operating System's AppData Directory

In a subfolder of:

- `AppData/Roaming` on Windows
- `~/Library/Application Support` on macOS
- `~/.config` on Linux a.k.a. `XDG_CONFIG_HOME`

* Good, because it's the idiomatic (intended) location for application data.
* Good, because it separates application data from user files, reducing clutter.
* Bad, because user might struggle to find these locations.

---------

# Additional Considerations

## Issue: Separate Per User and Per Machine Data

---------

Decision: Discussed
in [Meeting Notes: Default Storage Locations](../meeting-notes/0000-datastore-locations.md#default-storage-location). We
will pretend multi-user systems don't exist for now.

---------

!!! note "We will be using Windows as an example here, but this also applies to other operating systems"

The current issue with idiomatically using [AppData](#option-2-operating-systems-appdata-directory) is
the presence of multi user systems. For example, offices, LAN centres, etc.

There are two specific issues:

### 1. Network Synchronization

!!! Addressed

Files in `AppData/Roaming` are usually downloaded upon login in these configurations, and this download typically
happens on every login.

This is bad because it means having to potentially wait a very long time (even at Gigabit speed) to download a lot of
data on login. In the case of something like Starfield, game backup + mods could exceed 100GB, meaning a 10+ minute
download if no data is locally cached.

In the case of alternative [Home Directory](#option-1-users-home-directory) approach however, the home directory can
just be accessed over the network, avoiding the need for a long synchronization wait time. (At expense of slow
deployment times as mod archives would be accessed over the network)

### 2. Disk Space (Local)

In the other case of multiple users on same local machine, all mod + game backup data is duplicated on storage, wasting
potentially a huge amount of space.

### Additional Context

The idiomatic approach for this kind of problem is storing mods + backup game files
in a machine wide location such as `C:\ProgramData`. Per user data (loadouts, mod configs etc.) in `AppData/Roaming`.

When using in a multi-machine setup, like an office, the App would load the loadout from `AppData/Roaming` and start
downloading any mod assets it may be missing. If they are modding a game which has been modded by another user on the
same local system, the shared backup game files in `C:\ProgramData` would be used to first restore the game to its
original state.

Such as system may mean a slight rework of the DataStore however, as it means having to know where each file was
originally sourced from and (possibly) having a separate data store for user and machine wide data.

### The Alternative

Pretend the problem doesn't exist. Not uncommon in software development (sadly).
More convenient for developers, but the App however would work very poorly in multi user environments.

## Action Plan: Moving Default Configuration File

---------

Decision: [Portable mode to be implemented](../meeting-notes/0000-datastore-locations.md#portable-mode) with
`AppConfig.json` override method. Lack of `AppConfig.json` indicates 'use default paths'. Remaining settings
to be moved to DataStore.

For now, as a compromise, as not to not cause a regression in functionality, we will blank out the default
paths in the `AppConfig.json` file. If they are blank, we will apply the default paths decided upon in this ADR.

---------

Currently the App stores its configuration file (`AppConfig.json`) for custom paths in the same folder as the
application (`{EntryFolder}`). This is of course problematic, as many packaging systems make this folder read-only.

### Moving the Configuration File

Once changes tied to this ADR are applied, this config file should be moved to whatever is the new default decided
as a result of this ADR. If no file exists, the App should create one in the default location with the default values.

### Required: Support for 'Portable Mode'

For users who wish to run the App in 'portable mode' (e.g. in a non-static location), an override will need to exist to
read paths from the local folder (which is current behaviour).

This can be done in one of following ways:

- `AppConfig.json` in App folder takes precedence over default location `AppConfig.json`.
- `portable.txt` file forces a default set of 'portable' paths to be used.

The first option is more powerful, but the second option may be more familiar to users. In any case, implementing either
and switching between them code wise should be rather trivial.

!! danger

'Portable Mode' can only be supported for distribution methods where the App folder is writeable, for a list of these,
[see 'The Current Situation'](#the-current-situation). How we communicate this to the user is TBD.

## Future Questions: User Path Configuration UX

!! note "This is a pending discussion topic. To be moved to future ADRs."

Discussion on the User Experience of configuring paths is still pending.

Here are some (current) proposed discussion topics:

- How do we advertise 'portable' mode.
    - Either we copy the App files from a read-only directory to a new location, then tell user to uninstall their '
      installed' version.
    - Or users will need to download a separate build (e.g. AppImage instead of Flatpak, i.e. `ZIP` instead of `MSI` on
      Windows) to use it.
    - We can't have custom installer logic, as not all packaging formats support it. Do we make a custom installer?

- Support installing archives to multiple locations.
    - Do we do automated per game rules?
    - Size based rules?
    - Delegating to secondary locations if primary is full?

- Support dynamically moving the Archives between folders.
    - Do we use Steam inspired UI for this?
    - Reboot required?

---------

Decision: To be further discussed later. Location per game to be supported in the future.

---------
