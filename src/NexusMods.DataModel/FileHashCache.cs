using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.RateLimiting.Extensions;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Provides a fast lookup for hashes of existing files on storage by their absolute
/// path.
/// </summary>
/// <remarks>
///     Throughout the app we assume there will never be a duplicate hash.<br/><br/>
///     While in theory it is possible for there to be a duplicate; even with a
///     rough estimate of 300000, the possibility of collision (theoretical) is
///     still only 1 in 400 million [assuming perfect hash function].<br/><br/>
///
///     Related reading: <a href="https://en.wikipedia.org/wiki/Birthday_problem">Birthday Problem</a>
/// </remarks>
public class FileHashCache
{
    private readonly IResource<FileHashCache, Size> _limiter;
    private readonly IDataStore _store;

    /// <summary/>
    /// <param name="limiter">Limits CPU utilization where possible.</param>
    /// <param name="store">The store inside which the file hashes are kept within.</param>
    /// <remarks>
    ///    This constructor is usually called from DI.
    /// </remarks>
    public FileHashCache(IResource<FileHashCache, Size> limiter, IDataStore store)
    {
        _limiter = limiter;
        _store = store;
    }

    /// <summary>
    /// Tries to find a hash for the file from the cache.
    /// </summary>
    /// <param name="path">Path to the file to obtain from the cache.</param>
    /// <param name="entry">The obtained entry from the cache.</param>
    /// <returns></returns>
    /// <remarks>
    ///    When calling this code, you must ensure yourself that the returned cache
    ///    element is valid, by comparing last modified date and size with the actual
    ///    file on disk. If the file on disk does not match size and last modified,
    ///    you should call <see cref="IndexFileAsync"/>; which will update the underlying
    ///    cached item.
    /// </remarks>
    public bool TryGetCached(AbsolutePath path, out FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        Span<byte> span = stackalloc byte[Encoding.UTF8.GetByteCount(normalized)];
        Encoding.UTF8.GetBytes(normalized, span);
        var found = _store.GetRaw(IId.FromSpan(EntityCategory.FileHashes, span));
        if (found != null && found is not { Length: 0 })
        {
            entry = FileHashCacheEntry.FromSpan(found);
            return true;
        }
        entry = default;
        return false;
    }

    /// <summary>
    /// Asynchronously indexes the folder specified by <paramref name="path"/>; putting it in the cache.
    /// </summary>
    /// <param name="path">Path of the folder to hash.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Enumerator for all of the completed hash operations as they are available.</returns>
    /// <remarks>
    ///    Entries are pulled from cache if they already exist and we
    ///    can verify cached entry is accurate.
    /// </remarks>
    public IAsyncEnumerable<HashedEntry> IndexFolderAsync(AbsolutePath path, CancellationToken token = default)
    {
        return IndexFoldersAsync(new[] { path }, token);
    }

    /// <summary>
    /// Asynchronously indexes the folders specified by <paramref name="paths"/>; putting them in the cache.
    /// </summary>
    /// <param name="paths">Paths of the folders to hash.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>Enumerator for all of the completed hash operations as they are available.</returns>
    /// <remarks>
    ///    Entries are pulled from cache if they already exist and we
    ///    can verify cached entry is accurate.
    /// </remarks>
    public async IAsyncEnumerable<HashedEntry> IndexFoldersAsync(IEnumerable<AbsolutePath> paths, [EnumeratorCancellation] CancellationToken token = default)
    {
        // Don't want to error via a empty folder
        paths = paths.Where(p => p.DirectoryExists());

        var result = _limiter.ForEachFileAsync(paths, async (job, entry) =>
        {
            if (TryGetCached(entry.Path, out var found))
            {
                if (found.Size == entry.Size && found.LastModified == entry.LastWriteTimeUtc)
                {
                    job.ReportNoWait(entry.Size);
                    return new HashedEntry(entry, found.Hash);
                }
            }

            var hashed = await entry.Path.XxHash64Async(job, token);
            PutCachedAsync(entry.Path, new FileHashCacheEntry(entry.LastWriteTimeUtc, hashed, entry.Size));
            return new HashedEntry(entry, hashed);
        }, token, "Hashing Files");

        await foreach (var itm in result)
            yield return itm;
    }

    /// <summary>
    /// Asynchronously indexes the file specified by <paramref name="file"/>; putting them in the cache.
    /// </summary>
    /// <param name="file">Path of the file to be hashed.</param>
    /// <param name="token">Token to cancel the operation.</param>
    /// <returns>The individual hashed entry.</returns>
    /// <remarks>
    ///    Entry is pulled from cache if it already exists in the cache and we
    ///    can verify cached entry is accurate.
    /// </remarks>
    public async ValueTask<HashedEntry> IndexFileAsync(AbsolutePath file, CancellationToken token = default)
    {
        var info = file.FileInfo;
        var size = info.Size;
        if (TryGetCached(file, out var found))
        {
            if (found.Size == size && found.LastModified == info.LastWriteTimeUtc)
            {
                return new HashedEntry(file, found.Hash, info.LastWriteTimeUtc, size);
            }
        }

        using var job = await _limiter.BeginAsync($"Hashing {file.FileName}", size, token);
        var hashed = await file.XxHash64Async(job, token);
        PutCachedAsync(file, new FileHashCacheEntry(info.LastWriteTimeUtc, hashed, size));
        return new HashedEntry(file, hashed, info.LastWriteTimeUtc, size);
    }

    private void PutCachedAsync(AbsolutePath path, FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        Span<byte> kSpan = stackalloc byte[Encoding.UTF8.GetByteCount(normalized)];
        Encoding.UTF8.GetBytes(normalized, kSpan);
        Span<byte> vSpan = stackalloc byte[24];
        entry.ToSpan(vSpan);

        _store.PutRaw(IId.FromSpan(EntityCategory.FileHashes, kSpan), vSpan);
    }
}

/// <summary>
/// Represents an individual entry returned from the file cache.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Hash">The hash of the file.</param>
/// <param name="LastModified">The last time the entry was modified on disk.</param>
/// <param name="Size">Size of the file in bytes.</param>
public record HashedEntry(AbsolutePath Path, Hash Hash, DateTime LastModified, Size Size)
{
    /// <summary>
    /// Creates a hashed entry from an existing file entry obtained through a file search.
    /// </summary>
    /// <param name="fe">File entry to obtain the hashed entry from.</param>
    /// <param name="hash">The hash to create the entry from.</param>
    public HashedEntry(IFileEntry fe, Hash hash) : this(fe.Path, hash, fe.LastWriteTime, fe.Size) { }
}

/// <summary>
/// Represents an individual entry returned from the file cache as it is stored raw in the data store.
/// </summary>
/// <param name="Hash">The hash of the file.</param>
/// <param name="LastModified">The last time the entry was modified on disk.</param>
/// <param name="Size">Size of the file in bytes.</param>
public readonly record struct FileHashCacheEntry(DateTime LastModified, Hash Hash, Size Size)
{
    // TODO: SliceFast here, with only one size check for safety https://github.com/Nexus-Mods/NexusMods.App/issues/214

    /// <summary>
    /// Deserializes the given entry from a span of bytes.
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
    /// Serializes the current entry to a span of bytes.
    /// </summary>
    /// <param name="span">The span to serialize to.</param>
    public void ToSpan(Span<byte> span)
    {
        BinaryPrimitives.WriteInt64BigEndian(span, LastModified.ToFileTimeUtc());
        BinaryPrimitives.WriteUInt64BigEndian(span[8..], (ulong)Hash);
        BinaryPrimitives.WriteUInt64BigEndian(span[16..], Size.Value);
    }
}
