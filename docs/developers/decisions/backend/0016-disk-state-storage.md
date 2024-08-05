# Disk State Storage

## Context

During the life of a loadout, the application needs to track several the state of the disk. This data includes roughly the
following fields:

* Game Path - The path of a file, relative to a LocationId
* Hash - The xxHash64 of the file
* Size - The size of the file
* Last Modified - The last modified date of the file

The latter two entries are not often used directly but are used as shortcuts for detecting if a file needs to be rehashed,
a potentially expensive operation. At any time the application can know that it needs to rehash a file if the size or last
modified date has changed. We include the size in this calculation for use as a failsafe in the case that the user has
modified the file after resetting their system clock.

Additionally, when a loadout is applied to the game, at that point we want to track the state of the disk as the "last applied"
state, that can be used as part of the 3-way compare sync process. In addition, when we manage a game for the first time,
we want to record the state of the disk so that when we create a new loadout we can setup the state of the loadout to match
the unmodified state of the game.

## Important Discoveries During Design

During the design process for this new disk state a few important discoveries were made:

### All the disk states for a given game are linear in time

Since only one loadout can be applied at a time, and we try to sync loadouts and game state, we can view the disk state
as evolving linearly in time. This means that we can leverage MnemonicDB's linear time model to store pointers to transaction
and use those as the markers for the disk state. In other words, we don't need to store the disk state of every application of
a loadout, just the TX pointer to the disk state that was synced during the application of the loadout (more about this later)

### Disk state is sorted by the path

Since the paths in the disk state are stored in MnemonicDB, and since MnemonicDB sorts all data, when we query disk states we 
have an implicit ordering of all the paths. In addition, if we store these paths as `(GameId, LocationId, Path)` tuples,
or as `(LoadoutId, LocationId, Path)`, we can perform range queries of `(GameId, ...)` or `(LoadoutId, ...)` to get all the
entities that are relevant to a given game or loadout, sorted by path. This means that we then can perform a 3-way merge
join on the sources of the synchronizer, and remove a lot of the secondary indexing performed in previous implementations. 

In addition, if we query an `IndexSegment` for a given prefix, a binary search can be used to find specific entires in the
segment, reducing the need to scan the entire segment.

Finally, part of the synchronization process is to group loadout files by their location, and then select a winner from any
conflicting items. In the past this required a Linq GroupBy operation that caused a lot of overhead. With all the paths being
loaded pre-sorted, and in a single `IndexSegment`, the group-by can be performed by finding duplicate entries in the segment
and then passing around the group as a `start, end` range in the segment. Essentially this means that grouping can be done
on the stack instead of the heap.


## Implementation

The implementation of the disk state storage is as follows:

### GameMetadata

Each game installation, when detected, causes the creation of a `GameMetadata` entity. We attach disk state to this entity, and
this entity also contains pointers for the following:

* LastAppliedLoadout - The last loadout that was applied to the game
* LastAppliedLoadoutTransaction - The transaction that was created when the loadout was applied
* LastScannedDiskState - The transaction that was created when the disk state was last synced
* InitialStateTransaction - The transaction that was created when the game was first scanned
* DiskStateEntries (backref) - All the disk state entries that point to this game

### DiskStateEntry

Disk state entries are entities that contain the following fields:

* Path - (GameMetadataId, LocationId, RelativePath) the path of the file
* Hash - The xxHash64 of the file
* Size - The size of the file
* LastModified - The last modified date of the file
* GameMetadataId - The game that this disk state entry is associated with

### LoadoutItemWithTargetdPath

The LoadoutItemWithTargetPath model is also updated to use the `(LoadoutId, LocationId, RelativePath)` tuple for the target path,
so that at any time all the loadout items that reference a given target in a loadout can be queried via a range query.

## Code Implementation

Once this structure was created, many more parts of the application could be simplified. For example, the synchronizer
used to index data into several hashmaps, this can be replaced by a single range lookup in MnemonicDB. In addition the `Synchronize`
method is further simplified to update the disk state as it extracts, deletes, and otherwise updates files. No reason to 
re-scan the disk after synchronization, as the disk state is known when the modification to a given file are made.

At any point the disk state can be queried by `conn.AsOf(txId)` where `txId` is the transaction id of the desired point in time. 
Thus, to get the original disk state of a game, we can query from a database `conn.AsOf(game.InitialStateTransaction)`. And to
get the previously applied disk state, we can query `conn.AsOf(game.LastAppliedLoadoutTransaction)`.

## Implementation Status

All the above structure has been implemented, except for the 3-way merge join. This can be done later on in the development
process as the current implementation at least stores the disk state in the correct format. Likewise, the stack-based grouping
operation is not implemented.
