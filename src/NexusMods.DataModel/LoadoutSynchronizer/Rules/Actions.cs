namespace NexusMods.DataModel.LoadoutSynchronizer.Rules;

public enum Actions : ushort
{
    DoNothing = 1,
    /// <summary>
    /// Extracts the file specified in the loadout to disk, file should be previously deleted
    /// </summary>
    ExtractToDisk = 2,
    
    /// <summary>
    /// Deletes the file from disk
    /// </summary>
    DeleteFromDisk = 4,
    
    /// <summary>
    /// Deletes the file from the loadout via adding a reified delete
    /// </summary>
    AddReifiedDelete = 8,
    
    /// <summary>
    /// Updates the loadout to reference the new hash from the disk
    /// </summary>
    IngestHashChange = 16,
    
    /// <summary>
    /// A file would be extracted, but it's not archived, warn the user.
    /// </summary>
    WarnOfUnableToExtract = 32,
}
