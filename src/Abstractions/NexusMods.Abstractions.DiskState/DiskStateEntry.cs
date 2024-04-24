using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
/// Metadata about a file on disk.
/// </summary>
public readonly struct DiskStateEntry
{
    /// <summary>
    /// The hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// The size of the file.
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// The last modified time of the file.
    /// </summary>
    public required DateTime LastModified { get; init; }

    /// <summary>
    /// Converts a <see cref="HashedEntry"/> to a <see cref="DiskStateEntry"/>.
    /// </summary>
    /// <param name="hashedEntry"></param>
    /// <returns></returns>
    public static DiskStateEntry From(HashedEntryWithName hashedEntry)
    {
        return new()
        {
            Hash = hashedEntry.Hash,
            Size = hashedEntry.Size,
            LastModified = hashedEntry.LastModified
        };
    }

    /// <summary>
    /// Converts a <see cref="DiskStateEntry"/> to a <see cref="HashedEntry"/>.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public HashedEntryWithName ToHashedEntry(AbsolutePath path)
    {
        return new(path, Hash, LastModified, Size);
    }

}
