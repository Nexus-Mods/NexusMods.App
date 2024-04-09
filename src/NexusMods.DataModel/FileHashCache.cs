using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.DiskState;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Extensions.BCL;
using NexusMods.Extensions.Hashing;
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
public class FileHashCache : IFileHashCache
{
    /// <summary>
    /// The activity group for file hash cache activities.
    /// </summary>
    public static readonly ActivityGroup Group = ActivityGroup.From("FileHashCache");

    private readonly IActivityFactory _activityFactory;
    private readonly IDataStore _store;

    /// <summary/>
    /// <param name="activityFactory">Limits CPU utilization where possible.</param>
    /// <param name="store">The store inside which the file hashes are kept within.</param>
    /// <remarks>
    ///    This constructor is usually called from DI.
    /// </remarks>
    public FileHashCache(IActivityFactory activityFactory, IDataStore store)
    {
        _activityFactory = activityFactory;
        _store = store;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IAsyncEnumerable<HashedEntry> IndexFolderAsync(AbsolutePath path, CancellationToken token = default)
    {
        return IndexFoldersAsync(new[] { path }, token);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<HashedEntry> IndexFoldersAsync(IEnumerable<AbsolutePath> paths, [EnumeratorCancellation] CancellationToken token = default)
    {
        // Don't want to error via a empty folder
        var validPaths = paths.Where(p => p.DirectoryExists()).ToList();

        var allFiles = validPaths.SelectMany(p => p.EnumerateFiles())
            .Select(f => f.FileInfo)
            .ToList();

        using var activity = _activityFactory.Create<Size>(Group, "Hashing files in {Count} folders", validPaths.Count);

        activity.SetMax(allFiles.Sum(f => f.Size));

        var toPersist = new List<(IId, byte[] vSpan)>();
        var results = new ConcurrentBag<HashedEntry>();
        Parallel.ForEach(allFiles, info =>
        {
            if (TryGetCached(info.Path, out var found))
            {
                if (found.Size == info.Size && found.LastModified == info.LastWriteTimeUtc)
                {
                    results.Add(new HashedEntry(info, found.Hash));
                    return;
                }
            }
            // ReSharper disable once AccessToDisposedClosure
            var hashed = info.Path.XxHash64MemoryMapped(activity);
            lock (toPersist)
            {
                toPersist.Add(GetDbEntryToWrite(info.Path, new FileHashCacheEntry(info.LastWriteTimeUtc, hashed, info.Size)));
            }

            results.Add(new HashedEntry(info, hashed));
        }
        );

        // Insert all cached items into the DB.
        PutAllCached(toPersist); // <= required because this is async method
        foreach (var itm in results)
            yield return itm;
    }

    /// <inheritdoc/>
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

        using var job = _activityFactory.Create<Size>(Group, "Hashing {FileName}", file.FileName);
        job.SetMax(info.Size);
        var hashed = await file.XxHash64Async(job, token);
        PutCached(file, new FileHashCacheEntry(info.LastWriteTimeUtc, hashed, size));
        return new HashedEntry(file, hashed, info.LastWriteTimeUtc, size);
    }

    /// <inheritdoc/>
    public async ValueTask<DiskStateTree> IndexDiskState(GameInstallation installation)
    {
        var hashed =
            await IndexFoldersAsync(installation.LocationsRegister.GetTopLevelLocations().Select(f => f.Value))
                .ToListAsync();
        return DiskStateTree.Create(hashed.Select(h => KeyValuePair.Create(installation.LocationsRegister.ToGamePath(h.Path),
            DiskStateEntry.From(h))));
    }

    [SkipLocalsInit] // We don't need to zero the memory here
    private void PutCached(AbsolutePath path, FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        Span<byte> kSpan = stackalloc byte[Encoding.UTF8.GetByteCount(normalized)];
        Encoding.UTF8.GetBytes(normalized, kSpan);
        Span<byte> vSpan = stackalloc byte[24];
        entry.ToSpan(vSpan);

        _store.PutRaw(IId.FromSpan(EntityCategory.FileHashes, kSpan), vSpan);
    }
    
    private (IId, byte[] vSpan) GetDbEntryToWrite(AbsolutePath path, FileHashCacheEntry entry)
    {
        var normalized = path.ToString();
        var keyBytes = GC.AllocateUninitializedArray<byte>(Encoding.UTF8.GetByteCount(normalized));
        Encoding.UTF8.GetBytes(normalized, keyBytes);
        var valueBytes = GC.AllocateUninitializedArray<byte>(24);
        entry.ToSpan(valueBytes);

        return (IId.FromSpan(EntityCategory.FileHashes, keyBytes), valueBytes);
    }

    private void PutAllCached(List<(IId, byte[] vSpan)> toPersist)
    {
        var newItems = CollectionsMarshal.AsSpan(toPersist);
        _store.PutAllRaw(newItems);
    }
}
