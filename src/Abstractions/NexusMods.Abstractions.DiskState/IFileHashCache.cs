using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
///     Provides access to a cache where all datamodel recognised data lives.
/// </summary>
[Obsolete(message: "This will be removed")]
public interface IFileHashCache
{
    /// <summary>
    ///     Asynchronously indexes the folder specified by <paramref name="path" />; putting it in the cache.
    /// </summary>
    /// <remarks>
    ///     Entries are pulled from cache if they already exist and we
    ///     can verify cached entry is accurate.
    /// </remarks>
    IAsyncEnumerable<HashedEntryWithName> IndexFolderAsync(AbsolutePath path, CancellationToken token = default);

    /// <summary>
    ///     Indexes the folders a game installation and returns the disk state tree.
    /// </summary>
    ValueTask<DiskStateTree> IndexDiskState(GameInstallation installation);
}

/// <summary>
///     Represents an individual entry returned from the file cache.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Hash">The hash of the file.</param>
/// <param name="LastModified">The last time the entry was modified on disk.</param>
/// <param name="Size">Size of the file in bytes.</param>
public record HashedEntryWithName(AbsolutePath Path, Hash Hash, DateTime LastModified, Size Size)
{
    /// <summary>
    ///     Creates a hashed entry from an existing file entry obtained through a file search.
    /// </summary>
    /// <param name="fe">File entry to obtain the hashed entry from.</param>
    /// <param name="hash">The hash to create the entry from.</param>
    public HashedEntryWithName(IFileEntry fe, Hash hash) : this(fe.Path, hash, fe.LastWriteTime, fe.Size) { }
}

