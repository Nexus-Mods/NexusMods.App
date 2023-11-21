```
# These are optional elements. Feel free to remove any of them.
status: {proposed}
date: {2023-11-21 when the decision was last updated}
deciders: {App Team}
```

# Storage Locations for NexusMods.App Files

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
* Application data should be user accessible.
    * In other words, easy to find/navigate to.
    * Because application data, also contains logs and user accessible files.
    * e.g. If the App crashes on boot, finding logs should be easy.
* Cross-platform consistency
    * The paths should be consistent across different operating systems.
    * For example, if on one platform it is in entry directory, it should do the same on other platforms too.
* The user must have write access to the directory.

## Considered Options

* **Option 0: Keep using Entry Directory**
* **Option 1: User's Home Directory**
* **Option 2: Operating System's AppData Directory**

## Decision Outcome

{TBD}

### Consequences

{TBD}

## Pros and Cons of the Options

### Option 0: Keep using Entry Directory

* Good, because it makes the application portable (e.g. can carry on USB stick).
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

## Issue: Separate Per User and Per Machine Data

!!! note "We will be using Windows as an example here, but this also applies to other operating systems"

The current issue with idiomatically using [AppData](#option-2-operating-systems-appdata-directory) is
the presence of multi user systems. For example, offices, LAN centres, etc.

There are two specific issues:

***1. Network Synchronization***

Files in `AppData/Roaming` are usually downloaded upon login in these configurations, and this download typically
happens on every login.

This is bad because it means having to potentially wait a very long time (even at Gigabit speed) to download a lot of
data on login. In the case of something like Starfield, game backup + mods could exceed 100GB, meaning a 10+ minute
download if no data is locally cached.

In the case of alternative [Home Directory](#option-1-users-home-directory) approach however, the home directory can
just be accessed over the network, avoiding the need for a long synchronization wait time. (At expense of slow
deployment times as mod archives would be accessed over the network)

***2. Disk Space (Local)***

In the other case of multiple users on same local machine, all mod + game backup data is duplicated on storage, wasting
potentially a huge amount of space.

***Additional Context***

The idiomatic approach for this kind of problem is storing mods + backup game files
in a machine wide location such as `C:\ProgramData`. Per user data (loadouts, mod configs etc.) in `AppData/Roaming`.

When using in a multi-machine setup, like an office, the App would load the loadout from `AppData/Roaming` and start
downloading any mod assets it may be missing. If they are modding a game which has been modded by another user on the
same local system, the shared backup game files in `C:\ProgramData` would be used to first restore the game to its
original state.

Such as system may mean a slight rework of the DataStore however, as it means having to know where each file was
originally sourced from and (possibly) having a separate data store for user and machine wide data.

***The Alternative***

Pretend the problem doesn't exist. Not uncommon in software development (sadly).
More convenient for developers, but the App however would work very poorly in multi user environments.
