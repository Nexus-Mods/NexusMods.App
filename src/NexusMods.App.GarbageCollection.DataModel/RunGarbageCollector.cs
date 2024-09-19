using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Hashing.xxHash64;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
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
