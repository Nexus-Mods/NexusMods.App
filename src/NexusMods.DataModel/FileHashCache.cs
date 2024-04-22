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
using NexusMods.Extensions.Hashing;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
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
    private readonly IConnection _conn;

    /// <summary/>
    /// <param name="activityFactory">Limits CPU utilization where possible.</param>
    /// <param name="store">The store inside which the file hashes are kept within.</param>
    /// <remarks>
    ///    This constructor is usually called from DI.
    /// </remarks>
    public FileHashCache(IActivityFactory activityFactory, IConnection conn)
    {
        _activityFactory = activityFactory;
        _conn = conn;
    }

    /// <inheritdoc/>
    public bool TryGetCached(AbsolutePath path, out HashCacheEntry.Model entry)
    {
        var nameHash = path.ToString().XxHash64AsUtf8();
        var db = _conn.Db;
        var id = db
            .FindIndexed(nameHash, HashCacheEntry.NameHash)
            .FirstOrDefault(EntityId.MinValue);
        if (id == EntityId.MinValue)
        {
            entry = null!;
            return false;
        }

        entry = db.Get<HashCacheEntry.Model>(id);
        return true;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<HashedEntryWithName> IndexFolderAsync(AbsolutePath path, CancellationToken token = default)
    {
        return IndexFoldersAsync(new[] { path }, token);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<HashedEntryWithName> IndexFoldersAsync(IEnumerable<AbsolutePath> paths, [EnumeratorCancellation] CancellationToken token = default)
    {
        // Don't want to error via a empty folder
        var validPaths = paths.Where(p => p.DirectoryExists()).ToList();

        var allFiles = validPaths.SelectMany(p => p.EnumerateFiles())
            .Select(f => f.FileInfo)
            .ToList();

        using var activity = _activityFactory.Create<Size>(Group, "Hashing files in {Count} folders", validPaths.Count);

        activity.SetMax(allFiles.Sum(f => f.Size));

        var toPersist = new ConcurrentBag<HashedEntryWithName>();
        var results = new ConcurrentBag<HashedEntryWithName>();
        await Parallel.ForEachAsync(allFiles, (info, _) =>
        {
            if (TryGetCached(info.Path, out var found))
            {
                if (found.Size == info.Size && found.LastModified == info.LastWriteTimeUtc)
                {
                    results.Add(new HashedEntryWithName(info, found.Hash));
                    return ValueTask.CompletedTask;
                }
            }
            // ReSharper disable once AccessToDisposedClosure
            var hashed = info.Path.XxHash64MemoryMapped(activity);
            var result = new HashedEntryWithName(info, hashed);
            
            toPersist.Add(result);
            results.Add(result);
            return ValueTask.CompletedTask;
        }
        );

        // Insert all cached items into the DB.
        await PutAllCached(toPersist); // <= required because this is async method
        foreach (var itm in results)
            yield return itm;
    }

    /// <inheritdoc/>
    public async ValueTask<HashedEntryWithName> IndexFileAsync(AbsolutePath file, CancellationToken token = default)
    {
        var info = file.FileInfo;
        var size = info.Size;
        if (TryGetCached(file, out var found))
        {
            if (found.Size == size && found.LastModified == info.LastWriteTimeUtc)
            {
                return new HashedEntryWithName(file, found.Hash, info.LastWriteTimeUtc, size);
            }
        }

        using var job = _activityFactory.Create<Size>(Group, "Hashing {FileName}", file.FileName);
        job.SetMax(info.Size);
        var hashed = await file.XxHash64Async(job, token);
        var result = new HashedEntryWithName(file, hashed, info.LastWriteTimeUtc, size);
        await PutCached(file, result);
        return result;
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
    
    private async ValueTask PutCached(AbsolutePath path, HashedEntryWithName entry)
    {
        using var tx = _conn.BeginTransaction();
        var nameString = path.ToString();
        _ = new HashCacheEntry.Model(tx)
        {
            NameHash = nameString.XxHash64AsUtf8(),
            Name = nameString,
            LastModified = entry.LastModified,
            Hash = entry.Hash,
            Size = entry.Size,
        };
        await tx.Commit();
    }
    
    private async Task PutAllCached(ConcurrentBag<HashedEntryWithName> toPersist)
    {
        if (toPersist.Count == 0) return;
        using var tx = _conn.BeginTransaction();
        foreach (var itm in toPersist)
        {
            var stringName = itm.Path.ToString();
            _ = new HashCacheEntry.Model(tx)
            {
                NameHash = stringName.XxHash64AsUtf8(),
                Name = stringName,
                LastModified = itm.LastModified,
                Hash = itm.Hash,
                Size = itm.Size
            };
        }

        await tx.Commit();
    }
}
