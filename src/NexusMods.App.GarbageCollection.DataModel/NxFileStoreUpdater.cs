using NexusMods.Abstractions.FileStore.Nx.Models;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.FileProviders;
namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// This class is responsible for updating the file entries that are stored
/// in the Nx file store.
/// </summary>
public class NxFileStoreUpdater
{
    private readonly IConnection _connection;
    
    /// <summary/>
    public NxFileStoreUpdater(IConnection connection) {
        _connection = connection;
    }

    /// <summary>
    /// Updates the metadata used by the 'NxFileStore' to reflect the changes
    /// in <see cref="FileEntry"/> structures in the Nx archive.
    /// </summary>
    /// <param name="oldArchivePath">Path to the old archive file before repacking.</param>
    /// <param name="newArchivePath">The output of <see cref="NxRepacker.RepackArchive(System.IProgress{double},System.Collections.Generic.List{NexusMods.Hashing.xxHash3.Hash},System.Collections.Generic.List{NexusMods.Hashing.xxHash3.Hash},NexusMods.App.GarbageCollection.Structs.ArchiveReference{NexusMods.App.GarbageCollection.Nx.NxParsedHeaderState})"/>. This may be empty if the archive was deleted.</param>
    public void UpdateNxFileStore(List<Hash> toRetract, AbsolutePath oldArchivePath, AbsolutePath newArchivePath)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();
        var oldArchiveFileName = oldArchivePath.FileName;
        if (newArchivePath == default(AbsolutePath))
            goto archiveDoesNotExist;

        var fromAbsolutePathProvider = new FromAbsolutePathProvider
        {
            FilePath = newArchivePath,
        };
        var nxHeader = HeaderParser.ParseHeader(fromAbsolutePathProvider);

        foreach (var entry in nxHeader.Entries)
        {
            foreach (var archivedFile in ArchivedFile.FindByHash(db, (Hash)entry.Hash))
            {
                // Don't redirect if item already retracted or game doesn't belong
                // in the current container. We shouldn't mess with records that aren't
                // ours if the DataStore has duplicates due to developer error.

                // In case of duplicates, the GC will deduplicate files, the other
                // duplicates not from this container (archive) will be removed via
                // the `archiveDoesNotExist` section below.
                if (!archivedFile.IsValid() || !archivedFile.Container.Path.Equals((RelativePath)oldArchiveFileName))
                    continue;

                tx.Add(archivedFile.Id, ArchivedFile.NxFileEntry, entry);
                tx.Add(archivedFile.Container.Id, ArchivedFileContainer.Path, newArchivePath.Name);
            }
        }

        // Now retract all removed files
        archiveDoesNotExist:
        foreach (var retractedHash in toRetract)
        {
            foreach (var archivedFile in ArchivedFile.FindByHash(db, retractedHash))
            {
                // Don't retract if already retracted or game doesn't belong
                // in the current container. We must check container in case of
                // duplicates in the data store.
                if (!archivedFile.IsValid() || !archivedFile.Container.Path.Equals((RelativePath)oldArchiveFileName))
                    continue;

                archivedFile.Retract(tx);
            }
        }

        tx.Commit().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
