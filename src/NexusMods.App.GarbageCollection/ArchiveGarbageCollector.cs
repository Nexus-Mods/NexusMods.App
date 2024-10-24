using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NexusMods.App.GarbageCollection.Errors;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.App.GarbageCollection.Structs;
using NexusMods.App.GarbageCollection.Utilities;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
namespace NexusMods.App.GarbageCollection;

/// <summary>
///     This is the main entry point into the Garbage Collection mechanism.
///
///     The Garbage Collector runs in the following steps:
/// 
///     1. We perform a walk through the archive directory and collect
///        all the archive files and their hashes.
///
///     2. We then walk through the DataStore and collect all the hashes of
///        all files which are used. Incrementing the ref count of the existing
///        items in the DataStore.
///
///     3. Archives are then repacked, removing files which have a reference count of 0.
/// </summary>
public readonly struct ArchiveGarbageCollector<TParsedHeaderState, TFileEntryWrapper>
    where TParsedHeaderState : ICanProvideFileHashes<TFileEntryWrapper>
    where TFileEntryWrapper : IHaveFileHash
{
    /// <summary>
    ///     A mapping of all known file hashes to their respective archive
    ///     in which they are stored.
    /// </summary>
    internal readonly ConcurrentDictionary<Hash, ArchiveReference<TParsedHeaderState>> HashToArchive = new();

    /// <summary>
    ///     Stores all known archives.
    /// </summary>
    /// <remarks>
    ///     I (Sewer) chose this collection because it's low overhead on multi-thread access,
    ///     due to use of Interlocked.
    /// </remarks>
    internal readonly ConcurrentQueue<ArchiveReference<TParsedHeaderState>> AllArchives = new();

    /// <summary/>
    public ArchiveGarbageCollector() { }

    /// <summary>
    ///     Adds an individual archive to the collector.
    ///     [Thread Safe]
    /// </summary>
    public void AddArchive(AbsolutePath archivePath, TParsedHeaderState header)
    {
        var fileHashes = header.GetFileHashes();
        var entries = new Dictionary<Hash, HashEntry>(fileHashes.Length);
        var archiveReference = new ArchiveReference<TParsedHeaderState>
        {
            FilePath = archivePath,
            Entries = entries,
            HeaderState = header,
        };

        foreach (var fileHash in fileHashes)
        {
            entries[fileHash.Hash] = fileHash.Hash;
            HashToArchive[fileHash.Hash] = archiveReference;
        }

        AllArchives.Enqueue(archiveReference);
    }

    /// <summary>
    ///     Increments the ref count of a file from the data store.
    ///     [Thread Safe]
    /// </summary>
    /// <param name="hash">The hash for which the ref count is to be incremented.</param>
    /// <param name="throwOnUnknownFile">Throws if an unknown file is being added to the ref counter.</param>
    /// <exception cref="UnknownFileException">
    ///     An archive is being added via <see cref="AddReferencedFile"/> but
    ///     was not originally collected via <see cref="AddArchive"/>.
    /// </exception>
    public void AddReferencedFile(Hash hash, bool throwOnUnknownFile = false)
    {
        if (!HashToArchive.TryGetValue(hash, out var archiveRef))
        {
            if (throwOnUnknownFile)
                ThrowHelpers.ThrowUnknownFileException(hash);

            return;
        }

        lock (archiveRef!.Entries)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(archiveRef.Entries, hash);
            if (!Unsafe.IsNullRef(ref entry))
                entry.IncrementRefCount();
        }
    }

    /// <summary>
    ///     Collects all garbage from the archives.
    /// </summary>
    /// <param name="progress">
    ///     Reports progress of the repacking process.
    ///     As per convention, a value of 1 means 100% completion.
    ///     A value of 0 means 0% completion.
    /// </param>
    /// <param name="doRepack">
    ///     The method used to repack the archive. See the <see cref="RepackDelegate"/>
    ///     for more details.
    /// </param>
    public void CollectGarbage(IProgress<double>? progress, RepackDelegate doRepack)
    {
        var slicer = new ProgressSlicer(progress);
        var progressPerArchive = 1 / (double)AllArchives.Count;
        foreach (var archive in AllArchives)
        {
            var archiveProgress = slicer.Slice(progressPerArchive);
            var toArchive = new List<Hash>(archive.Entries.Count);
            var toRemove = new List<Hash>(archive.Entries.Count);
            foreach (var item in archive.Entries)
            {
                if (item.Value.GetRefCount() >= 1)
                    toArchive.Add(item.Key);
                else
                    toRemove.Add(item.Key);
            }

            var shouldRepack = toArchive.Count != archive.Entries.Count;
            if (!shouldRepack)
                continue;

            doRepack(archiveProgress, toArchive, toRemove, archive);
        }
        
        progress?.Report(1);
    }
    
    /// <summary>
    ///     Performs the repacking of an individual archive.
    /// </summary>
    /// <param name="progress">
    ///     Reports progress of the repacking process.
    /// </param>
    /// <param name="toArchive">
    ///     The list of hashes as the placed in the new archive.
    ///     The archive will be repacked with only the hashes in this list.
    /// </param>
    /// <param name="toRemove">
    ///     The list of hashes to be removed from the new archive.
    /// 
    ///     Combined with <paramref name="toArchive"/> this contains all hashes
    ///     in the archive.
    ///
    ///     These files by definition have a ref count of 0; thus are not used anywhere.
    /// </param>
    /// <param name="archive">
    ///     The archive to be repacked.
    /// </param>
    public delegate void RepackDelegate(IProgress<double> progress, List<Hash> toArchive, List<Hash> toRemove, ArchiveReference<TParsedHeaderState> archive);
}
