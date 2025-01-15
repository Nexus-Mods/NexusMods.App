# Updating Mods

!!! info "This is a design document detailing the steps taken to update mods."

A corresponding research document (original design) can be found on a [separate page][research-doc].

## General Approach

!!! tip "First read the [Problem Statement] in the [Research Document]"

The requested approach (from business) has been to maximize the use of the V2 API,
as opposed to programming against the legacy V1 API wherever possible.

!!! info "We will wait for the 'Mods 2.0' project if possible"

    Including how `Mod Updates` are to be handled in the future.

To promote an iterative development and speed up production, this feature will be developed in
'phases'. Each phase will deliver a Minimum Viable Product (MVP) for a given feature set, then
once all initial functionality is completed, various aspects will be refined.

### Step 1: Updating Mod Page Info

For now, we will:

- [1. Determine Updated Mod Pages], to update our local cache.
- [2. Multi Query Pages], for update mod pages with a 'cache miss'.

### Step 2: Mapping Old Files to New Files

!!! info "Once we have the updated mod pages, we can determine the updates available for files"

- [Use the 'fuzzy' search strategy](#fuzzy-search-strategy) to match files. (Phase 0)

***Once all other work around updates (UI, etc.)*** is complete, with a working prototype, we will
improve on this by doing the following (Phase 1):

- [Use the `file_updates` array from V1 API's Querying Mod Files][querying-mod-files]
    - Or an equivalent V2 API, if available.

## Fuzzy Search Strategy

!!! info "This is a 'cheap' strategy to try detect file updates in the presence of [missing update links][querying-mod-files]"

Basically we match uploads by file name; trying to match mods together.

Mods file names can be generally divided into the following classes:

- Consistent-ish naming, with version in mod name (e.g. [Skyrim 202X](#reference-example-skyrim-202x)).
- Constant naming e.g. [USSEP](#reference-example-ussep)
- File Names with Substituted Characters e.g. [SkyUI](#reference-example-skyui)
- File Versions with non-semver suffixes e.g. [A Quality World Map](#reference-example-a-quality-world-map)
- File Names with File Extensions [Maestros of Synth](#reference-example-maestros-of-synth)

The strategy is the following:

1. Try to parse version strings into variants that may appear in file name:
    - Make it semver compliant (e.g. `5.2SE` -> `5.2`)
    - With stripped version prefix (e.g. `v5.2` -> `5.2`)
    - With substituted underscores (e.g. `5.2` -> `5_2`)
2. Strip each of the above strings from the file name (loop, case insensitive).
3. Strip known file extensions from end (e.g. `Maestros of Synth.zip` -> `Maestros of Synth`)
4. Substitute underscores with spaces (e.g. `Ava_Complexions` -> `Ava Complexions`)
    - In case mod author mixed spaces and underscores between mod authors.
5. Remove runs of spaces.
6. Match old file to current file by what remains of file name (case insensitive).
    - Where file is newer and version string is newer (if possible to compare).

### Reference Example: [Skyrim 202X]

!!! info "Example with consistent naming for full releases, but incosistent naming for non-full releases."

v9 Release:

```
-Skyrim 202X 9.0 - Architecture PART 1
-Skyrim 202X 9.0 - Landscape PART 2
-Skyrim 202X 9.0 - Other PART 3
```

v10 Release:

```
-Skyrim 202X 10.0 - Architecture PART 1
-Skyrim 202X 10.0 - Landscape PART 2
-Skyrim 202X 10.0 - Other PART 3
```

v10 Release (2):

```
-Skyrim 202X 10.0.1 - Architecture PART 1
-Skyrim 202X 10.0.1 - Landscape PART 2
-Skyrim 202X 10.0.1 - Other PART 3
```

v10 Patches

```
-Skyrim 202X 10.1 Update
-Skyrim 202X 10.2 Update
-Skyrim 202X 10.3 Update
-Skyrim 202X 10.4 Update Solstheim [<= Does not contain files from other update packages]
```
!!! note "Skyrim 202X 10.4 Update Solstheim does is meant to be installed over `10.3`"

    Rather than standalone. Edge cases like this is why we prefer to avoid false positives.

In this case, our strategy extracts the strings:

```
-Skyrim 202X - Architecture PART 1
-Skyrim 202X - Landscape PART 2
-Skyrim 202X - Other PART 3
```

Meaning files can be matched without correct `file_updates` mappings, even in presence of multiple
'parts' with same version number.

### Reference Example: [USSEP]

!!! info "Single mod, all same name, but version is provided in file name."

All versions:

```
Unofficial Skyrim Special Edition Patch
```

In this case our logic still sees 

```
Unofficial Skyrim Special Edition Patch
```

As the file names are all the same, it's an automatic match by file version.

### Reference Example: [SkyUI]

!!! info "Example case where part of the file name was substituted"

```
SkyUI_5_2_SE
SkyUI_5_1_SE
```

e.g. Spaces, dots, were substituted with underscores. More common with old/older files, rather than
recent ones.

In this case the name becomes

```
SkyUI SE
SkyUI SE
```

We did the substitution of version to underscores and stripped it from the input.

Notably the version string here is `5.2SE`, so we also need to filter out characters in version
strings.

### Reference Example: [A Quality World Map]

!!! info "Example where there are multiple variants of a mod, with differing version strings"

```
9.0 A Quality World Map - Paper
9.0 A Quality World Map - Vivid with Flat Roads
9.0 A Quality World Map - Vivid with Stone Roads
```

With the following version strings:

```
9.0P
9.0VF
9.0
```

In this case, you want to filter out the 'P' and 'VF' in the version strings, turning our
file names to:

```
A Quality World Map - Paper
A Quality World Map - Vivid with Flat Roads
A Quality World Map - Vivid with Stone Roads
```

Then we can compare versions.

### Reference Example: [Maestros of Synth]

```
Maestros of Synth.zip
Maestros of Synth
```

In this case, we strip file names:

```
Maestros of Synth
Maestros of Synth
```

[Problem Statement]: ../../misc/research/00-update-implementation-research.md#problem-statement
[1. Determine Updated Mod Pages]: ../../misc/research/00-update-implementation-research.md#1-determine-updated-mod-pages
[2. Multi Query Pages]: ../../misc/research/00-update-implementation-research.md#multi-query-pages
[querying-mod-files]: ../../misc/research/00-update-implementation-research.md#2-querying-mod-files
[Research Document]: ../../misc/research/00-update-implementation-research.md
[research-doc]: ../../misc/research/00-update-implementation-research.md
[Skyrim 202X]: https://www.nexusmods.com/skyrimspecialedition/mods/2347?tab=files
[USSEP]: https://www.nexusmods.com/skyrimspecialedition/mods/266?tab=files
[SkyUI]: https://www.nexusmods.com/skyrimspecialedition/mods/12604?tab=files
[A Quality World Map]: https://www.nexusmods.com/skyrimspecialedition/mods/5804?tab=files
[Maestros of Synth]: https://www.nexusmods.com/cyberpunk2077/mods/3776?tab=files