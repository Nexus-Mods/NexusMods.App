namespace NexusMods.Sdk.FileStore;

/// <summary>
/// Defines a relative priority for a stream source. Lower values are higher priority.
/// Mostly this is used so that we attempt to use backed-up files first, then files found
/// other places on-disk, and then finally files that are downloaded from the internet.
/// </summary>
public enum SourcePriority : byte
{
    Backup = 1,
    Local = 10,
    Remote = 100,
}
