using NexusMods.Abstractions.Installers.DTO.Files;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Games.DTO;

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
    public static DiskStateEntry From(HashedEntry hashedEntry)
    {
        return new()
        {
            Hash = hashedEntry.Hash,
            Size = hashedEntry.Size,
            LastModified = hashedEntry.LastModified
        };
    }

    /// <summary>
    /// Converts a <see cref="StoredFile"/> to a <see cref="DiskStateEntry"/>.
    /// </summary>
    /// <param name="hashedEntry"></param>
    /// <returns></returns>
    public static DiskStateEntry From(StoredFile hashedEntry)
    {
        return new()
        {
            Hash = hashedEntry.Hash,
            Size = hashedEntry.Size,
            LastModified = DateTime.Now
        };
    }

}
