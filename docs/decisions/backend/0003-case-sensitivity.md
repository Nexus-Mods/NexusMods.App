# Case Sensitivity and Paths

## Context and Problem Statement

In general Windows, and Windows based games expect case insensitivity. While Linux and OSX in general use case insensitivity. Many of these differences are papered
over by Wine but since the app will be running outside of Wine's Windows API emulation, we will have to deal with these changes ourselves

## Decision Drivers

We need a set of libraries and a way of working that allows us to code with some sort of sanity while being assured that the games will function correctly under Wine.
If we can't enforce this santiy via types we'll need to develop a coding and design pattern that when "cargo culted" results in the correct behvior. 

We also assume that having two files with the *same name* in the *same folder* that differ only via capitalization is an anti-pattern and is not to be supported. If
a mod tries to install `foo.md` and `fOo.md`, this is considered degenerative behavior and is not support (and we should recommend that the Nexus web APIs be updated 
to reject such files)

## Key observations

The main issue with path differences comes from a single situation: looking up a file on disk by name. We can compare insensitively in memory and in our data stores,
but if we ever ask the OS "give me file X" or "list the files in in folder Y" we have to be 100% sure of the capitalization of that folder. 

Most of the app is based on looking up existing paths, comparing them to a desired result and then doing something to those paths. This is good, it means we can often
devine the correct casing in situations where it matters. If the file exists we can use the existing path casing, if it does not exist we can assume that whatever is
reading it expects insensitivity because it is running through Wine. We only need to worry about duplicating folders and pre-existing files when extractin mods.

## Decision Outcome

1) `AbsolutePaths` **shall** always carry the casing and path separators of the OS, we will not convert them while in memory to some standard format. They will not be parsed into some internal format in-memory. They will exist in memory as the OS expects
2) `AbsolutePaths` that come from the OS **should** be prefered over `.Join`'ing an AbsolutePath to a RelativePath. 
3) All paths *shall* compare in a separator and casing insensitive manner
  * `c:\foo` *shall* be equal to `c:/foo`
  * `/foo/Bar` *shall* be equal to `/foo/bar`
  * `/foo\bar` *shall* be equal to `\foo\bar`
4) Joining an `AbsolutePath` to a `RelativePath` *shall not* not be via the same method as joining two relative paths.
  * `absPath.CombineChecked(relPath)` will walk the resultant path and emit a path with the correct capitalization and path separators
  * If a folder does not exist it *shall* be assumed to be in the format of the `RelativePath`
5) Since `.CombineChecked` is such a heavy operation, scanning a directory and matching existing AbsolutePaths in memory is prefered over calling `.CombineChecked`
6) Pull Requests that contain code with calls to `.CombineChecked` will be heavily scrutinized to find situations where the usage can be reduced or further limited.
7) Conversion of strings into `AbsolutePath`s will likewise be highly scrutinized to ensure that they only ever reference paths that come from the OS itself
8) In general the coding "mantra" will be: "Look before you leap". In other words, enumerate the files in a folder and use the `AbsolutePath`s you read to drive your code. Prefer this over combining paths and then checking for their existance. 
 
### Consequences

With the above checks and design plan, we can code internally as if all paths are case insensitive. We no longer have to worry about trying to open an non-existent file as the creation of absolute paths out of "thin air" is restricted.
There may still be cases were two calls to `.CombineChecked` will attempt to create two folders with different casing at the same time, but since calls to `.CreateChecked` are heavily controlled we should be able to limit these situations. 
Two files having the same casing is considered degenerate behavior and so should not pose a problem. 

Since all operations that involve creating files on the game folders, where case sensitivity is an issue, happen inside `loadout.Apply` we can restrict how we operate on absolute paths to this folder. 

This *does* mean that if we want to get an `AbsolutePath` to `{Game}\bin\x64\Cyberpunk2077.exe` the only reliable way to do so is to call a scan of the entire folder and do the match in memory. Thus
we *will* update all the game support contracts to prefer this method. Having OS-specific fast directory listing will be very helpful here.
