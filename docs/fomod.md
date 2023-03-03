# Intro

FOMOD describes a set of installer formats that are implemented largely independent though they share some of their interface to the main application.
It's the successor to OMOD.
Where OMOD is a custom archive format using "deflate" (?) for compression, FOMOD uses standard .zip, .rar or .7z files and requires a certain directory structure for the installer files to be detectable.

Especially in older mods, a common pattern was for the archive to be given a .fomod extension when it was really just a .zip file.
Sometimes mods contain a nested archive, so the outer archive could be a .rar file containing a single zip file with .fomod extension which then contains the actual mod content.

Despite the name, these formats are not actually restricted to certain games though they offer functionality that make no sense outside the gamebryo engine and may lack functionality that would be useful for other games.

## XML

This is a descriptive format, a data file describes installation steps and options that can be selected within each steps. More complex logic can
be created through various types of dependencies between options, entire steps may only appear based on prior choices (i.e. having an "Defaults" vs "Custom" paths).

Unfortunately this format is not a fully specified standard, especially on the topic of priorities between multiple options containing the same files, the existing spec is fuzzy such that different managers, all following the spec correctly, can produce different results.

This format is only supported in FOMOD (?)

## C#

With these installers the mod archive contains a file with c# source code implementing a predefined interface.
The mod manager has to compile the script and then load&execute it within a .NET environment.
The installer itself is arbitrary C# code (technically targeting .net framework 4.x) that can produce its own UI (usually using System.Forms directly) but the interface also offers some functionality to interact with the mod manager.

Because the script is arbitrary code that gets compiled on the user system and run in the context of the mod manager (which is not uncommon to be run as administrator), the security risk from these installers shouldn't be underestimated.
NMM and Vortex run these installers in a security sandbox for this reason - which in turn can lead to compatibility problems.

Since these scripts are compiled against assemblies of the core application and effectively run as a plugin within its context, this can also lead to problems if "tree shaking" is applied to the application as functionality may be required by the script that isn't used by the app itself and could thus get removed.

This format is supported in FOMOD and OMOD (?) but has seen little use recently.

## ModScript

Similar to the above, this is a script based installer, using a DSL (Domain Specific Language) this time.
It being more restrictive in what it can do makes this less of a security concern and theoretically it could probably be supported without relying on the .NET runtime.

This format is theoretically supported in OMOD and FOMOD but never used in FOMOD in practice (?)

## info.xml

On top of the actual installer script, fomods may contain an "info.xml" file which contains meta information about the mod, like a name, author, version number, ...
Since this information is redundant to what Nexus Mods reports through the api, the information on the page is easier to update and there is no smart way to deal with contradictions between these data sources, Vortex at least largely ignores this file.

# Supporting the different formats

## OMOD

Currently out of scope

## FOMOD

### Files required during installation

#### XML

- fomod/ModuleConfig.xml (or fomod/script.xml (?), never seen this in the wild)
- theoretically any image (don't have to be in an "images" subdirectory)

#### C#

- fomod/script.cs
- theoretically any other file in the archive

#### ModScript

?

# Interface to the mod manager

During installation the script may interact with the main application, e.g. to display a UI, to check the existence of files, ...

These functions can be grouped in the following way:

## Interface: UI

As mentioned earlier, C# scripts will usually render their own UI using .NETs System.Forms directly.
Both C# script and ModScript installers can also use a higher-level, simpler api to offer selection between multiple options but this was never supported in Vortex and never missed so there are no known examples of C# scripts actually using this interface.

For XML based installers, this interface is quite abstract,
the installer provides a tree structure of installation steps > groups (aka "optionalFileGroup", aka "plugin group" within the xml spec) > options (aka "plugins")
FOMM, NMM, MO2 and Vortex all present these with one screen per step, all groups and options within are displayed at once.

Based on whether certain files exist in the current mod loadout and the selection on previous steps, further steps, groups and options may be hidden, disabled or preselected.

What the Mod Manager UI has to display is thus a function of the relevant attributes in the installer, the environment as far as the interface allows querying it and the options selected in the installer so far.
Any change in the selection has to trigger a re-evaluation of all conditions in the installer and thus an update of the "effective" option tree.

The following attributes control the visual presentation:

### Top Level
- Module Name?
- Module Image?
- -> StepList

### StepList
- order? (Ascending!, Descending, Explicit)
- -> Step[]

### Step
- visible (condition)
- name
- -> GroupList

### GroupList
- order?
- -> Group[]

### Group
- name
- type (SelectAtLeastOne, SelectAtMostOne, SelectExactlyOne, SelectAll, SelectAny)
- -> OptionList

### OptionList
- order?
- -> Option[]

### Option
- name
- description?
- image?
- type? (Required, Optional, Recommended, NotUsable, CouldBeUsable)

Beyond this, each option can have a list of files associated with that option.
This is not currently displayed (by neither NMM nor Vortex). Files don't _have_ to be associated with an option directly, instead they can depend on flags _set_ such that options will indirectly lead to files getting installed. Therefore this file list would often be incomplete and misleading.

## Interface: Plugins

This interface allows installers to query the existence of plugin (esp, esm, esl, specific to bethesda games) and whether they are enabled in the load order or not.

- is plugin present (args: filename; returns: true/false)
- is plugin active (args: filename; returns: true/false)
- get full list of plugins (args: onlyActive; returns: list of names) (not used in xml installers)

## Interface: Ini Files

C# based scripts retrieve info from ini files

- get ini int (args: filename, section, key; returns: int)
- get ini string (args: filename, section, key; returns: string value)

(scripts can access specialised versions of these functions for the bethesda games but this is abstracted away in the library)

## Interface: Environment

- app version (returns: version string of running application)
- game version (returns: version string of the game being managed)
- script extender version (args: extender id; returns: version string of the extender, if installed)
- is extender present (returns: true/false) (c# only)
- file exists (args: filepath; returns: true/false) (c# only)
- get data file content (args: filepath; returns: byte[]) (c# only)
- get data file list (args: path, filter, isRecursive,; returns: list of names) (c# only)

## Output

In the original codebase (FOMM, NMM), certain operations, e.g. extracting files, editing ini files, ..., would be performed immediately during the mod installation with no way of reversing if an error was encountered further down the line.
Vortex, and I suggest NMA, delegate everything, even from the c# scripts, meaning the installer script generates a list of "outputs" which the manager could then review and then actually "perform" in a separate step. The installer is therefore not supposed to have side effects.

This means that some interfaces that, from the perspective of the script, "perform" an action (e.g. "make change x to ini file y"), under the hood only "queue" that action at the discretion of the mod manager, delegated to after the script has fully run its course.

As such, the output of the installer is a list of "instructions" of these types:
- extract file from archive (args: from, to, priority)
- create empty directory (args: to)
- generate file from in-memory data (args: to, data)
- edit ini file (args: file, section, key, value)
- enable plugin (args: name)
- enable all plugins
- error (args: severity, message)
- unsupported function called (args: function)

There are a few instructions that Vortex never supported that weren't really missed:
- set plugin order index (args: plugin, index)
- set relative load order (args: list of plugins)

It may seem weird to tread "error" and "unsupported function call" but since there is no interaction between core and installer that would allow an error to be handled and the process to be resumed, effectively the error is either fatal in which the script exits early with no way to recover or it continued and it's basically up to the user to decide what to do with the error message.
