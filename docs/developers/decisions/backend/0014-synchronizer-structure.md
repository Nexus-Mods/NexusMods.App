# Structure of the Synchronizer

## Context and Problem Statement

For the first part of development (until the start of alpha) the app was based on a two-method model. At various parts of the
application either the `Apply` or `Ingest` methods would be called which would either sync from the loadout to the disk, or pull
changes from the disk into the loadout. This model was fairly simple, was better suited for the fully immutable, git-like,
every loadout is a fork, model that we were using at the time. With the introduction of MnemonicDB, we moved to a structure
where a loadout was treated less like `git` and more like a financial ledger. As the saying goes: "accountants use ink". 
That is to say, there is no editing of the past, only the adding of new data. 

The comparison between git and a ledger is best explained this way: in git, every change to a folder results in a new reality, 
the old reality exists, and there is no direct connection between the two. The contents of each "reality" are hashed and the
name of the reality is the hash of the top level folder (the commit SHA). So there is no way in this model to know that one
file in one SHA is the same as another, except by comparing the contents and the path. In a ledger, each account has a specific
id. Changing the balance of the account involves adding a transaction, and the current state of the account is the sum of all
transactions. Every account has an id that sticks with it for the entire life of the account.

This impacts the design of the synchronizer when it comes with how to deal with data external to the application. In a git-like
model, an apply and ingest operation result in a fork of the loadout. Then later on if two loadouts need to be reconciled, 
they would be merged into a single loadout. This is very close to the fork/merge model of git. In a ledger model, any changes
to the loadout must be reconciled to the ledger at transaction time.

## Three-way merge

The cleanest way to perform this reconciliation is to have a three-way merge, that examines the current loadout, the disk,
and the last applied state of the loadout. Any changes on disk can be detected by comparing them to the previously applied state.

As an example, lets setup a situation where we will look at the state of the disk, previous state, and the loadout as a tuple.
In this shorthand we will use `A` and `B` to represent hashes, and `x` to represent a missing file. Based on this we can 
create some example states:

- xxA - The file exists in the Loadout, but not on disk and was not in the previous state, so it should be extracted to disk
- xAB - The file exists in the Loadout and the previous state, but the file hash has changed, so it should be extracted to disk
- Axx - The file exists on disk, and not in the Loadout or the previous state, so it's a new file that should be added to the Loadout
- ABB - The file exists in all 3 locations, but the file on disk has changed after it was last applied, so it should be ingested
- ABC - This is a conflict, it's not clear what should be done here, so the user should be prompted to resolve the conflict

## Decision Drivers
When deciding how to perform this 3 way merge, previous failures to implement a 3 way merge were considered for the reasons
why they failed. The main issue was the complexity of writing all the possible states. Later on we'll see that we have around
92 possible states, and handling these as a set of `if-then-else` trees is a nightmare. 

In addition, this sync process needed to be fast enough to handle large numbers of files, and be side-effect free in the analysis
phase. Several parts of the app display the possible changes that will be made if a sync is performed, and these UI elements
depend on the sync being side effect free and fast.

Since there are 92 states, it was desirable to have all the possible combinations and mappings to actions exist in a single
file, with an optimal one-line-per state mapping. This would allow for easy debugging and testing of the sync process.

## Implementation Details
Early on in the design process it was discovered that most of the states in the three way merge consist of boolean flags. And that
the entire process roughly breaks down into 3 phases:

1) Gather information
2) Calculate the operations to be performed
3) Perform the operations

If phases 1 and 2 are completely side effect free, then the UI can use just the first two phases to display the changes that will
be performed, and then only perform the execution phase if the user agrees to the changes.

If every possible state is boiled down to a set of boolean flags, then all the possible states can be represented as a single
unsigned integer, and that integer can be mapped to a set of operations. If these operations are themselves represented as
boolean flags, then all mappings can be expressed as a dictionary of integers to integers. In this design the state of the 
of a file is known as the "Signature" of the file, and the resulting actions are known as `Actions`. They are both represented
in C# as an ushort flag enum.

### Signature Builder

To create a signature, the helper struct known as `SignatureBuilder` was created. This struct can be filled out with the
correct data, then the `.Build()` method can be called to create the signature. The signature looks like the following:

```csharp
[Flags]
public enum Signature : ushort
{
    /// <summary>
    /// Empty signature, used only as a way to detect an uninitialized signature.
    /// </summary>
    Empty = 0,
    
    /// <summary>
    /// True if the file exists on disk.
    /// </summary>
    DiskExists = 1,
    
    /// <summary>
    /// True if the file exists in the previous state.
    /// </summary>
    PrevExists = 2,
    
    /// <summary>
    /// True if the file exists in the loadout.
    /// </summary>
    LoadoutExists = 4,
    
    /// <summary>
    /// True if the hashes of the disk and previous state are equal.
    /// </summary>
    DiskEqualsPrev = 8,
    
    /// <summary>
    /// True if the hashes of the previous state and loadout are equal.
    /// </summary>
    PrevEqualsLoadout = 16,
    
    /// <summary>
    /// True if the hashes of the disk and loadout are equal.
    /// </summary>
    DiskEqualsLoadout = 32,
    
    /// <summary>
    /// True if the file on disk is already archived.
    /// </summary>
    DiskArchived = 64,
    
    /// <summary>
    /// True if the file in the previous state is archived.
    /// </summary>
    PrevArchived = 128,
    
    /// <summary>
    /// True if the file in the loadout is archived.
    /// </summary>
    LoadoutArchived = 256,
    
    /// <summary>
    /// True if the path is ignored, i.e. it is on a game-specific ignore list.
    /// </summary>
    PathIsIgnored = 512,
}
```

As one can see, the entire state is represented in 10 bits of integers, which does mean that all the possible states of this
bit field are roughly one million. However in practice several of these flags are mutually exclusive or imply other flags. For 
example, if the disk and loadout are equal, and the disk and previous are equal, then the loadout and previous are equal. In addition,
if the loadout does not have an entry, there is no way the entry can be archived. Reducing all these illogical states reduces
the search space down to 92 states. These states are represented in another enum known as the `SignatureShorthand` enum. And
are simply pre-defined enum values that are easier for humans to digest than a raw bitfield, or jagged lines of or-ing flags.

```csharp
/// <summary>
/// Summary of all 92 possible signatures in a somewhat readable format
/// </summary>
public enum SignatureShorthand : ushort
{
	/// <summary>
	/// LoadoutExists
	/// </summary>
	xxA_xxx_i = 0x0004,
	/// <summary>
	/// LoadoutExists, LoadoutArchived
	/// </summary>
	xxA_xxX_i = 0x0104,
	/// <summary>
	/// LoadoutExists, PathIsIgnored
	/// </summary>
	xxA_xxx_I = 0x0204,
}
```

Both the SignatureShorthand enum and Actions enum are combined in the `ActionMapping` class as a large switch statement:

```csharp

   public static Actions MapAction(SignatureShorthand shorthand)
    {
        // Format of the shorthand:
        // xxx_yyy_z -> xxx: Loadout, yyy: Archive, z: Ignore path
        // xxx: a tuple of `(Disk, Previous, Loadout)` states.
        // A `x` means no state because that source has no value for the path.
        // A `A`, `B`, `C` are placeholders for the hash of the file, so `AAA` means all three sources have the same hash, while `BAA` means the hash is different on disk
        // from either the previous state or the loadout.
        // yyy: a tuple of `(Disk, Previous, Loadout)` archive states. `XXX` means all three sources are archived (regardless of their hash) and `Xxx` means the disk is archived but the previous and loadout states are not.
        // `z`: either `i` or `I`, where `i` means the path is not ignored and `I` means the path is ignored.
        // The easiest way to think of this is that a capital letter means the existence of data, while a lowercase letter means the absence of data or a false value.
        return shorthand switch
        {
            xxA_xxx_i => WarnOfUnableToExtract,
            xxA_xxX_i => ExtractToDisk,
            xxA_xxx_I => WarnOfUnableToExtract,
            xxA_xxX_I => ExtractToDisk,
            xAx_xxx_i => DoNothing,
}
```

The TL;DR of all this, is that if you want to edit the behavior of the synchronizer, look at the contents of `ActionMapping.cs` and 
modify the mappings for the desired state.

## High-level design
All of this behavior is encapsulated in the `ALoadoutSynchronizer` class. The previous phases of flattening loadouts and sorting
mods is replaced with the following phases:

* BuildSyncTree
  * All files in the loadout are grouped by their path
  * Any files in the groups that are in a mod that is disabled are filtered out
  * Any group that has more than one file is handed to the `SelectWinningFile` virtual method which returns a single that is conisered
the winning override for the given path. 
  * The winning file is then added to the sync tree under the path of the group
  * The previous state of the game folders is then added to the sync tree indexed by the game paths
  * The disk state of the game folders is then added to the sync tree indexed by the game paths
  * The sync tree then is a combination of the three tree states
* ProcessSyncTree
  * Each entry in the sync tree is loaded into a `SignatureBuilder`.
  * Hashes for each entry are sent to the virtual method `HaveArchive` to determine if the file is archived
  * The `SignatureBuilder` is then built and the resulting signature added to the sync tree
  * The signature is then mapped to an action using the `ActionMapping` class
  * As the actions are calculated, each node in the tree is added to an action grouping, which is a bag of SyncTreeNodes indexed by
the action that should be performed. A given node can be in multiple action groups if it has multiple actions that should be performed.
* RunGroupings
  * Each group of actions is then executed in order. The order of the actions is determined by the value of the action in the `Actions` enum
  * When a new file is ingested, it is added to the `Overrides` mod. After all new files are added to Overrides, the files are handed to the virtual method
`MoveNewFilesToMods` which allows any inheriting class to move the files to game specific mods.
  * Some actions may modify the loadout, the new loadout is returned from the action

