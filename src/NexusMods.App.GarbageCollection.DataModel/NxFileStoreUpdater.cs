using NexusMods.Abstractions.FileStore.Nx.Models;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Hashing.xxHash64;
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
    /// <param name="newArchivePath">The output of <see cref="NxRepacker.RepackArchive(System.IProgress{double},System.Collections.Generic.List{NexusMods.Hashing.xxHash64.Hash},System.Collections.Generic.List{NexusMods.Hashing.xxHash64.Hash},NexusMods.App.GarbageCollection.Structs.ArchiveReference{NexusMods.App.GarbageCollection.Nx.NxParsedHeaderState})"/></param>
    public void UpdateNxFileStore(List<Hash> toRetract, AbsolutePath newArchivePath)
    {
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();
        if (newArchivePath == default(AbsolutePath))
            goto archiveDoesNotExist;

        var fromAbsolutePathProvider = new FromAbsolutePathProvider { FilePath = newArchivePath };
        var nxHeader = HeaderParser.ParseHeader(fromAbsolutePathProvider);

        foreach (var entry in nxHeader.Entries)
        {
            foreach (var archivedFile in ArchivedFile.FindByHash(db, (Hash)entry.Hash))
            {
                tx.Add(archivedFile.Id, ArchivedFile.NxFileEntry, entry);
                tx.Add(archivedFile.Container.Id, ArchivedFileContainer.Path, newArchivePath.Name);
            }
        }
        
        // Now retract all removed files
        archiveDoesNotExist:
        foreach (var retractedHash in toRetract)
        {
            foreach (var archivedFile in ArchivedFile.FindByHash(db, retractedHash))
                archivedFile.Retract(tx);
        }

        tx.Commit().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}
