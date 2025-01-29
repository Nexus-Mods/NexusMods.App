using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games.FileHashes;

/// <summary>
/// Interface for the file hashes service, which provides a way to download and update the file hashes database
/// </summary>
public interface IFileHashesService
{
    /// <summary>
    /// Force an update of the file hashes database
    /// </summary>
    public Task CheckForUpdate(bool forceUpdate = false);
    
    /// <summary>
    /// Get the file hashes database, downloading it if necessary
    /// </summary>
    public ValueTask<IDb> GetFileHashesDb();
}
