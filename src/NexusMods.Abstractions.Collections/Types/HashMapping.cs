using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Collections.Types;

/// <summary>
/// Metadata about a mapping from a MD5 hash to a xxHash64 hash and the size of the file.
/// </summary>
public struct HashMapping
{
    /// <summary>
    /// The xxHash64 hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }
    
    /// <summary>
    /// The size of the file.
    /// </summary>
    public required Size Size { get; init; }
}
