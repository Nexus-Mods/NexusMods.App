namespace NexusMods.Abstractions.Loadouts.Synchronizers.Rules;

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
    /// Deletes the file from the loadout via adding a reified delete
    /// </summary>
    AddReifiedDelete = 1 << 3,

    /// <summary>
    /// Deletes the file from disk
    /// </summary>
    DeleteFromDisk = 1 << 4,

    /// <summary>
    /// Extracts the file specified in the loadout to disk, file should be previously deleted
    /// </summary>
    ExtractToDisk = 1 << 5,

    /// <summary>
    /// A file would be extracted, but it's not archived, warn the user.
    /// </summary>
    WarnOfUnableToExtract = 1 << 6,

    /// <summary>
    /// Warn the user that the merge has failed and we don't know what to do with the file
    /// </summary>
    WarnOfConflict = 1 << 7,
}
