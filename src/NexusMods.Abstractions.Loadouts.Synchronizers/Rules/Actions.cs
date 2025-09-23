namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

/// <summary>
/// Actions that the synchronizer can perform on the file. The order of these actions
/// is (unfortunately) conflated with the order of the flags. This means that if you
/// want a certain ordering to the actions (like backing up a file before deleting it)
/// you should ensure that the flags reflect this ordering
/// </summary>
[Flags]
public enum Actions : ushort
{
    /// <summary>
    /// Do nothing, should not be combined with any other action
    /// </summary>
    DoNothing = 1 << 0,

    /// <summary>
    /// Create a backup of the file
    /// </summary>
    BackupFile = 1 << 1,

    /// <summary>
    /// Updates the loadout to reference the new hash from the disk
    /// </summary>
    IngestFromDisk = 1 << 2,
    
    /// <summary>
    /// Adapts the loadout to match the contents of the intrinsic files on disk
    /// </summary>
    AdaptLoadout = 1 << 3,

    /// <summary>
    /// Deletes the file from the loadout via adding a reified delete
    /// </summary>
    AddReifiedDelete = 1 << 4,

    /// <summary>
    /// Deletes the file from disk
    /// </summary>
    DeleteFromDisk = 1 << 5,

    /// <summary>
    /// Extracts the file specified in the loadout to disk, file should be previously deleted
    /// </summary>
    ExtractToDisk = 1 << 6,
    
    /// <summary>
    /// Writes the intrinsic files to disk, should be done after extraction
    /// </summary>
    WriteIntrinsic = 1 << 7,

    /// <summary>
    /// A file would be extracted, but it's not archived, warn the user.
    /// </summary>
    WarnOfUnableToExtract = 1 << 8,

    /// <summary>
    /// Warn the user that the merge has failed and we don't know what to do with the file
    /// </summary>
    WarnOfConflict = 1 << 9,
}
