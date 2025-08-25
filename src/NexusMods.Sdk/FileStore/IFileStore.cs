using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.Sdk.Threading;

namespace NexusMods.Sdk.FileStore;

/// <summary>
/// Takes hashes and files and stores them in a way that can be retrieved later on. Essentially this is a
/// de-duplicating Key/Value store, where the keys are hashes and the values are the file contents.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Returns true if there is an archive that has the specified file.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public ValueTask<bool> HaveFile(Hash hash);

    /// <summary>
    /// Backup the given set of files.
    ///
    /// If the size or hash do not match during the
    /// backup process an exception may be thrown.
    /// </summary>
    /// <param name="backups">The files to back up.</param>
    /// <param name="deduplicate">Ensures no duplicate files are stored.</param>
    /// <param name="token"></param>
    /// <remarks>
    /// Backing up duplicates should generally be avoided, as it encurs a performance and
    /// disk space penalty. However accidentally creating a duplicate is not
    /// considered a failure case; the Garbage Collector is equipped to deal
    /// with duplicates.
    ///
    /// As a default <paramref name="deduplicate"/> is set to 'true' to avoid duplicates,
    /// however it is slightly more efficient to set <paramref name="deduplicate"/> to 'false'
    /// and manually check for duplicates with <see cref="HaveFile"/> API when constructing
    /// the <paramref name="backups"/> collection.
    ///
    /// The <c>BackupFiles</c> itself is thread safe, but duplicates may be made
    /// if called from duplicate threads at once. This can prevent with taking a lock
    /// via <see cref="Lock"/> (and `using` statement). That said, the probability of duplicates
    /// being made without a lock is so low that it is generally recommended not to lock
    /// to instead maximize performance. The Garbage Collector will remove any duplicates down the road.
    /// </remarks>
    Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, bool deduplicate = true, CancellationToken token = default);

    /// <summary>
    /// Similar to <c>BackupFiles</c> except the same archive is used.
    /// </summary>
    Task BackupFiles(string archiveName, IEnumerable<ArchivedFileEntry> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract the given files to the given disk locations, provide as a less-abstract interface in case
    /// the extractor needs more direct access (such as memory mapping).
    /// </summary>
    /// <param name="files"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExtractFiles(IEnumerable<(Hash Hash, AbsolutePath Dest)> files, CancellationToken token = default, Action<(int Current, int Max)>? progress = null);

    /// <summary>
    /// Extract the given files from archives.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Dictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default);

    /// <summary>
    /// Gets a read-only seekable stream for the given file.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Stream> GetFileStream(Hash hash, CancellationToken token = default);

    /// <summary>
    /// Load the given file into memory.
    /// </summary>
    Task<byte[]> Load(Hash hash, CancellationToken token = default);

    /// <summary>
    /// Locks the file store, preventing it from being used until the returned
    /// <see cref="IDisposable"/> is disposed.
    /// </summary>
    AsyncFriendlyReaderWriterLock.WriteLockDisposable Lock();

    /// <summary>
    /// Reload any caches that may be stale due to GC operations
    /// </summary>
    void ReloadCaches();
}



