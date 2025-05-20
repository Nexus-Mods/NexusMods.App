using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// A grouping of multiple hashes for a single piece of data.
/// </summary>
public record MultiHash
{
    /// <summary>
    /// The xxHash3 hash of the data.
    /// </summary>
    public required Hash XxHash3 { get; init; }
    
    /// <summary>
    /// The xxHash64 hash of the data.
    /// </summary>
    public required Hash XxHash64 { get; init; }
    
    /// <summary>
    /// the minimal hash of the data.
    /// </summary>
    public required Hash MinimalHash { get; init; }
    
    /// <summary>
    /// The Sha1 hash of the data.
    /// </summary>
    public required Sha1 Sha1 { get; init; }
    
    /// <summary>
    /// The Md5 hash of the data.
    /// </summary>
    public required Md5 Md5 { get; init; }
    
    /// <summary>
    /// The Crc32 hash of the data.
    /// </summary>
    public required Crc32 Crc32 { get; init; }
    
    /// <summary>
    /// The size of the data in bytes.
    /// </summary>
    public required Size Size { get; init; }
}
