using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NexusMods.App.GarbageCollection.Errors;
using NexusMods.App.GarbageCollection.Interfaces;
using NexusMods.App.GarbageCollection.Structs;
using NexusMods.App.GarbageCollection.Utilities;
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
    /// <exception cref="UnknownFileException">
    ///     An archive is being added via <see cref="AddReferencedFile"/> but
    ///     was not originally collected via <see cref="AddArchive"/>.
    /// </exception>
    public void AddReferencedFile(Hash hash)
    {
        if (!HashToArchive.TryGetValue(hash, out var archiveRef))
            ThrowHelpers.ThrowUnknownFileException(hash);

        lock (archiveRef!.Entries)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(archiveRef.Entries, hash);
            entry.IncrementRefCount();
        }
    }
}
