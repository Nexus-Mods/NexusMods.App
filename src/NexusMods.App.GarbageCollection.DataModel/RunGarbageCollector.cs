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
    public static void Do(ISettingsManager settingsManager, NxFileStore store, NxFileStoreUpdater updater, IConnection connection)
    {
        using var lck = store.LockForGC();
        var gc = new ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper>();
        DataStoreNxArchiveFinder.FindAllArchives(settingsManager, gc);
        DataStoreReferenceMarker.MarkUsedFiles(connection, gc);
        gc.CollectGarbage(new Progress<double>(), (progress, toArchive, toRemove, archive) =>
        {
            NxRepacker.RepackArchive(progress, toArchive, toRemove, archive, true, out var newArchivePath);
            updater.UpdateNxFileStore(toRemove, newArchivePath);
        });
    }
}
