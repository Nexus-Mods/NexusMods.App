using NexusMods.Hashing.xxHash64;

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
}
