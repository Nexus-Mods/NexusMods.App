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
    ///     you should call <see cref="FileHashCache.IndexFileAsync" />; which will update the underlying
    ///     cached item.
    /// </remarks>
    bool TryGetCached(AbsolutePath path, out FileHashCacheEntry entry);

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
    IAsyncEnumerable<HashedEntry> IndexFolderAsync(AbsolutePath path, CancellationToken token = default);

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
    IAsyncEnumerable<HashedEntry> IndexFoldersAsync(IEnumerable<AbsolutePath> paths,
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
    ValueTask<HashedEntry> IndexFileAsync(AbsolutePath file, CancellationToken token = default);

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
public record HashedEntry(AbsolutePath Path, Hash Hash, DateTime LastModified, Size Size)
{
    /// <summary>
    ///     Creates a hashed entry from an existing file entry obtained through a file search.
    /// </summary>
    /// <param name="fe">File entry to obtain the hashed entry from.</param>
    /// <param name="hash">The hash to create the entry from.</param>
    public HashedEntry(IFileEntry fe, Hash hash) : this(fe.Path, hash, fe.LastWriteTime, fe.Size) { }
}

/// <summary>
///     Represents an individual entry returned from the file cache as it is stored raw in the data store.
/// </summary>
/// <param name="Hash">The hash of the file.</param>
/// <param name="LastModified">The last time the entry was modified on disk.</param>
/// <param name="Size">Size of the file in bytes.</param>
public readonly record struct FileHashCacheEntry(DateTime LastModified, Hash Hash, Size Size)
{
    // TODO: SliceFast here, with only one size check for safety https://github.com/Nexus-Mods/NexusMods.App/issues/214

    /// <summary>
    ///     Deserializes the given entry from a span of bytes.
    /// </summary>
    /// <param name="span">The span of bytes to deserialize from.</param>
    public static FileHashCacheEntry FromSpan(ReadOnlySpan<byte> span)
    {
        var date = BinaryPrimitives.ReadInt64BigEndian(span);
        var hash = BinaryPrimitives.ReadUInt64BigEndian(span[8..]);
        var size = BinaryPrimitives.ReadInt64BigEndian(span[16..]);
        return new FileHashCacheEntry(DateTime.FromFileTimeUtc(date), Hash.FromULong(hash), Size.FromLong(size));
    }

    /// <summary>
    ///     Serializes the current entry to a span of bytes.
    /// </summary>
    /// <param name="span">The span to serialize to.</param>
    public void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteInt64BigEndian(span, LastModified.ToFileTimeUtc());
        BinaryPrimitives.WriteUInt64BigEndian(span[8..], (ulong)Hash);
        BinaryPrimitives.WriteUInt64BigEndian(span[16..], Size.Value);
    }
}
