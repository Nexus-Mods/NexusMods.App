using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.Threading;

namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// Utility class for running the garbage collector against the DataModel and
/// Nx archives.
/// </summary>
public static class RunGarbageCollector
{
    private static readonly AsyncFriendlyReaderWriterLock _gcLock = new();
    
    /// <summary/>
    /// <param name="archiveLocations">The archive locations, usually obtained from 'DataModelSettings'.</param>
    /// <param name="store">The <see cref="IFileStore"/> that requires locking for concurrent access.</param>
    /// <param name="connection">The MneumonicDB <see cref="IConnection"/> to the DataModel.</param>
    public static void Do(Span<ConfigurablePath> archiveLocations, IFileStore store, IConnection connection)
    {
        // See 'SAFETY' comment below for explanation of 'gcLock'
        using var gcLock = _gcLock.WriteLock();
        var toUpdateInDataStore = new List<ToUpdateInDataStoreEntry>();

        using (store.Lock())
        {
            var gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
            DataStoreNxArchiveFinder.FindAllArchives(archiveLocations, gc);
            DataStoreReferenceMarker.MarkUsedFiles(connection, gc);
            gc.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archive) =>
            {
                NxRepacker.RepackArchive(progress, toArchive, toRemove, archive, false, out var newArchivePath);
                toUpdateInDataStore.Add(new ToUpdateInDataStoreEntry(toRemove, archive.FilePath, newArchivePath));
            });
        }

        // SAFETY: Updating the FileStore interacts with external non-GC components,
        //         such as MnemonicDB. This may cause us to yield to external code
        //         that could touch the FileStore lock. To avoid deadlocks, we should
        //         prevent this from happening if possible.
        //
        //         This is why we release `store.Lock()` early.
        
        // NOTE:   In theory UpdateNxFileStore can call GC back again. This is unlikely to happen
        //         however for the time being; because we only run GC when deleting a library item
        //         or loadout. No callback should do that. Long term we want to prevent re-entrancy.
        //
        //         Running arbitrary code in GC in any system is however prone to possible failure,
        //         so long term we will want to avoid UpdateNxFileStore (MnemonicDB Commit) to avoid
        //         yielding to external code. We need a non-blocking `Commit`; that
        //         sends stuff off to another thread or internal queue without blocking.
        var updater = new NxFileStoreUpdater(connection);
        foreach (var entry in toUpdateInDataStore)
        {
            updater.UpdateNxFileStore(entry.ToRemove, entry.OldFilePath, entry.NewFilePath);
            // Delete original archive. We do this in a delayed fashion such that
            // a power loss during the UpdateNxFileStore operation does not lead
            // to an inconsistent state
            entry.OldFilePath.Delete();
        }
    }

    private record struct ToUpdateInDataStoreEntry(List<Hash> ToRemove, AbsolutePath OldFilePath, AbsolutePath NewFilePath);
}
