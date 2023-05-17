using NexusMods.DataModel.Loadouts.ApplySteps;
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
    Task BackupFiles(IEnumerable<(AbsolutePath, Hash, Size)> backups, CancellationToken token = default);
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="files"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default);
}
