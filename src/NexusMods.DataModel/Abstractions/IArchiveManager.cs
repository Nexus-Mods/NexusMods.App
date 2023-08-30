using NexusMods.Common;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.DataModel.Abstractions;

/// <summary>
/// Manager for archive files such as downloads and backups.
/// </summary>
public interface IArchiveManager
{

    /// <summary>
    /// Returns true if there is an archive that has the specified file.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public ValueTask<bool> HaveFile(Hash hash);

    /// <summary>
    /// Backup the given files. If the size or hash do not match during the
    /// backup process a exception may be thrown.
    /// </summary>
    /// <param name="backups"></param>
    /// <param name="token"></param>
    Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, CancellationToken token = default);


    /// <summary>
    /// Extract the given files to the given disk locations, provide as a less-abstract interface incase
    /// the extractor needs more direct access (such as memory mapping).
    /// </summary>
    /// <param name="files"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default);

    /// <summary>
    /// Extract the given files from archives.
    /// </summary>
    /// <param name="files"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default);

    /// <summary>
    /// Gets a read-only seekable stream for the given file.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<Stream> GetFileStream(Hash hash, CancellationToken token = default);
}


/// <summary>
/// A helper class for <see cref="IArchiveManager"/> that represents a file to be backed up. The Path is optional,
/// but should be provided if it is expected that the paths will be used for extraction or mod installation.
/// </summary>
/// <param name="Stream"></param>
/// <param name="Hash"></param>
/// <param name="Size"></param>
/// <param name="Path"></param>
public readonly record struct ArchivedFileEntry(IStreamFactory StreamFactory, Hash Hash, Size Size);
