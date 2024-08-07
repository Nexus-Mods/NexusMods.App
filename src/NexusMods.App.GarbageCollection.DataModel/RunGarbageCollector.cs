using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.DataModel;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// Utility class for running the garbage collector against the DataModel and
/// Nx archives.
/// </summary>
public static class RunGarbageCollector
{
    /// <summary/>
    /// <param name="archiveLocations">The archive locations, usually obtained from <see cref="DataModelSettings"/>.</param>
    /// <param name="store">The <see cref="NxFileStore"/> that requires locking for concurrent access.</param>
    /// <param name="connection">The MneumonicDB <see cref="IConnection"/> to the DataModel.</param>
    public static void Do(Span<ConfigurablePath> archiveLocations, NxFileStore store, IConnection connection)
    {
        using var lck = store.LockForGC();
        var updater = new NxFileStoreUpdater(connection);
        var gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        DataStoreNxArchiveFinder.FindAllArchives(archiveLocations, gc);
        DataStoreReferenceMarker.MarkUsedFiles(connection, gc);
        gc.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archive) =>
        {
            NxRepacker.RepackArchive(progress, toArchive, toRemove, archive, true, out var newArchivePath);
            updater.UpdateNxFileStore(toRemove, newArchivePath);
        });
    }
}
