using NexusMods.Abstractions.GameLocators;
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

    /// <summary>
    /// Get the files associated with a specific game
    /// </summary>
    public IEnumerable<GameFileRecord> GetGameFiles(IDb referenceDb, GameInstallation installation, string[] commonIds);
    
    /// <summary>
    /// The current file hashes database, will thrown an error if not initialized via GetFileHashesDb first.
    /// </summary>
    public IDb Current { get; }

    /// <summary>
    /// Lookup a game version string from a given game installation and locator metadata
    /// </summary>
    public string GetGameVersion(GameInstallation installation, IEnumerable<string> locatorMetadata);

    /// <summary>
    /// Get the common IDs for a specific version of a given game installation
    /// </summary>
    public bool TryGetCommonIdsForVersion(GameInstallation gameInstallation, string version, out string[] commonIds);
}
