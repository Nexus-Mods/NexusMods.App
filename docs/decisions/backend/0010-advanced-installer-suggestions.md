# Advanced Installer: Suggestions

This document describes the design of the 'Suggestions' system within Advanced Installer, pictured
in the following mockup below:

![](./images/0009-advanced-installer-location.png)

## Context Statement

When using the Advanced Installer, the user should be provided with 'hints' dictating where a file might
require to be placed based on a number of heuristics.

This is part of the UX effort to 'Make Modding Easy' and a general requirement of our [Advanced Installer Design](./0009-advanced-installer-design.md).

## Considered Options (Suggestions)

- Reusing the Deployment System (`InstallFolderTarget`) for suggestions.
- Creating a Suggestion System from scratch.

## Decision Outcome

Rather than splitting the metadata in two, we can leverage the existing
`InstallFolderTarget` system in order to implement Advanced Installer suggestions;
as that system already has required metadata.

### Consequences

- [Good] Strong code reuse as `InstallFolderTarget` already has required metadata to support this functionality.
- [Neutral] Each `InstallFolderTarget` folder will now need a description.
- [Neutral] All games will need to be converted to new `InstallFolderTarget` system.

## Implementation Algorithm (Directories)

Note: String comparisons performed here adhere to ![Paths Doc](./0003-paths.md). [Case insensitive + / separator]

The suggestion algorithm for folders is described below.
We will use modified example from [Advanced Installer Design](./0009-advanced-installer-design.md).

Consider the following directory structure:

```text
Red Hair
├─ Data
│  ├─ model.dae
│  └─ hair.dds
Green Hair
├─ Data
│  ├─ model.dae
│  └─ hair.dds
Blue Hair
└─ Data
   ├─ model.dae
   └─ hair.dds
```

Assume the `Data` folder is a folder within the game directory (also applies to subfolders).

If the user selects `model.dae` in `Blue Hair`; the suggestion algorithm should try finding the following in the game's directory tree:
- `Blue Hair/Data`
- `Data`

We keep removing the first directory in the path, until there's at least 1 match in the game's directory tree
(substring sanitized path!). Once there is at least 1 match, include that in the suggestions and terminate the search.

tl;dr Include any folders where any `substring` of the in-archive directory path, matches an `InstallFolderTarget`.

## Implementation Algorithm (Files)

The suggestion algorithm for files functions in the following manner:

- Create initial list of 'suggestions' from all `InstallFolderTarget`(s) recursively.
- Filter by `KnownValidFileExtensions`. (Remove if unknown extension).
- Filter by `FileExtensionsToDiscard`. (Remove if extension to discard).
- Add any directories from [Implementation Algorithm (Directories)](#implementation-algorithm-directories)

## Acquiring `InstallFolderTarget` During Deploy Step (a.k.a. `GetModsAsync`)

Extend the `IGame` interface to expose `InstallFolderTarget` for the game's most common directories.
Then populate that with `InstallFolderTarget`(s) used in the game's common mod installers.

This interface can be accessed during the deploy step under `GameInstallation` structure.
