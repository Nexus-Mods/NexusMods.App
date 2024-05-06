namespace NexusMods.Abstractions.MnemonicDB.Attributes;

/// <summary>
/// Predefined partitions for the Ids in the app, these have no semantic meaning,
/// but they help to optimize the cache, as entities of the same partition will be
/// grouped together in the indexes. Under-partitioning of Ids could result in some
/// cache fragmentation.
/// </summary>
public enum IdPartitions : byte
{

    /// <summary>
    /// The partition for the HashCache entries
    /// </summary>
    HashCache = 16,
    
    /// <summary>
    /// DiskState entities
    /// </summary>
    DiskState,
    
    /// <summary>
    /// Downloaded files and their contents
    /// </summary>
    DownloadAnalysis,
}
