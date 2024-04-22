using System.Buffers.Binary;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.DiskState;

/// <summary>
///     Provides access to a cache where all datamodel recognised data lives.
/// </summary>
public interface IFileHashCache
{
    /// <summary>
    ///     Tries to find a hash for the file from the cache.
    /// </summary>
    /// <param name="path">Path to the file to obtain from the cache.</param>
    /// <param name="entry">The obtained entry from the cache.</param>
    /// <returns></returns>
    /// <remarks>
    ///     When calling this code, you must ensure yourself that the returned cache
    ///     element is valid, by comparing last modified date and size with the actual
    ///     file on disk. If the file on disk does not match size and last modified,
    ///     you should call <see cref="IFileHashCache.IndexFileAsync" />; which will update the underlying
    ///     cached item.
    /// </remarks>
    bool TryGetCached(AbsolutePath path, out HashCacheEntry.Model entry);

    /// <summary>
    ///     Asynchronously indexes the folder specified by <paramref name="path" />; putting it in the cache.
    /// </summary>
    /// <param name="path">Path of the folder to hash.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Enumerator for all of the completed hash operations as they are available.</returns>
    /// <remarks>
    ///     Entries are pulled from cache if they already exist and we
    ///     can verify cached entry is accurate.
    /// </remarks>
    IAsyncEnumerable<HashedEntryWithName> IndexFolderAsync(AbsolutePath path, CancellationToken token = default);

    /// <summary>
    ///     Asynchronously indexes the folders specified by <paramref name="paths" />; putting them in the cache.
    /// </summary>
    /// <param name="paths">Paths of the folders to hash.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Enumerator for all of the completed hash operations as they are available.</returns>
    /// <remarks>
    ///     Entries are pulled from cache if they already exist and we
    ///     can verify cached entry is accurate.
    /// </remarks>
    IAsyncEnumerable<HashedEntryWithName> IndexFoldersAsync(IEnumerable<AbsolutePath> paths,
        CancellationToken token = default);

    /// <summary>
    ///     Asynchronously indexes the file specified by <paramref name="file" />; putting them in the cache.
    /// </summary>
    /// <param name="file">Path of the file to be hashed.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>The individual hashed entry.</returns>
    /// <remarks>
    ///     Entry is pulled from cache if it already exists in the cache and we
    ///     can verify cached entry is accurate.
    /// </remarks>
    ValueTask<HashedEntryWithName> IndexFileAsync(AbsolutePath file, CancellationToken token = default);

    /// <summary>
    ///     Indexes the folders a game installation and returns the disk state tree.
    /// </summary>
    /// <param name="installation"></param>
    /// <returns></returns>
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

