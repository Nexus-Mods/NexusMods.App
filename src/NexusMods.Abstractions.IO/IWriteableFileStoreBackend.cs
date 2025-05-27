namespace NexusMods.Abstractions.IO;

/// <summary>
/// Interface for a file store backend that allows backing up of (and retrieving) files.
/// </summary>
public interface IWriteableFileStoreBackend : IReadableFileStoreBackend
{
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
    /// The <see cref="BackupFiles"/> itself is thread safe, but duplicates may be made
    /// if called from duplicate threads at once. This can prevent with taking a lock
    /// via <see cref="Lock"/> (and `using` statement). That said, the probability of duplicates
    /// being made without a lock is so low that it is generally recommended not to lock
    /// to instead maximize performance. The Garbage Collector will remove any duplicates down the road.
    /// </remarks>
    Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, bool deduplicate = true, CancellationToken token = default);
}
