using NexusMods.Common;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

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
    Task BackupFiles(IEnumerable<(IStreamFactory, Hash, Size)> backups, CancellationToken token = default);
    
    
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
}
