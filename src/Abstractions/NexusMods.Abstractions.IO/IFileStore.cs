using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.Abstractions.IO;

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
/// A helper class for <see cref="IFileStore"/> that represents a file to be backed up. The Path is optional,
/// but should be provided if it is expected that the paths will be used for extraction or mod installation.
/// </summary>
/// <param name="StreamFactory"></param>
/// <param name="Hash"></param>
/// <param name="Size"></param>
public readonly record struct ArchivedFileEntry(IStreamFactory StreamFactory, Hash Hash, Size Size);
