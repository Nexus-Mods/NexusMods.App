using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Hashing.xxHash3;
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
    /// Get all the supported game versions for a given game installation
    /// </summary>
    public IEnumerable<string> GetGameVersions(GameInstallation installation);
    
    /// <summary>
    /// Get the file hashes database, downloading it if necessary
    /// </summary>
    public ValueTask<IDb> GetFileHashesDb();

    /// <summary>
    /// Get the files associated with a specific game. The LocatorIds are opaque ids that come from a game locator.
    /// For steam these will be manifestIDs, for GOG they will be buildIDs, etc.
    /// </summary>
    public IEnumerable<GameFileRecord> GetGameFiles(GameInstallation installation, IEnumerable<string> locatorIds);
    
    /// <summary>
    /// The current file hashes database, will thrown an error if not initialized via GetFileHashesDb first.
    /// </summary>
    public IDb Current { get; }

    /// <summary>
    /// Try to get the game version for a given game installation and locator IDs
    /// </summary>
    public bool TryGetGameVersion(GameInstallation installation, IEnumerable<string> locatorIds, out string version);
    
    /// <summary>
    /// Get the locator IDs for a specific version of a given game installation
    /// </summary>
    public bool TryGetLocatorIdsForVersion(GameInstallation gameInstallation, string version, out string[] commonIds);
    
    /// <summary>
    /// Suggest a game version based on the files in a game installation
    /// </summary>
    public string SuggestGameVersion(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files);

    /// <summary>
    /// Return the locator IDs for a specific version definition given a game installation
    /// </summary>
    public string[] GetLocatorIdsForVersionDefinition(GameInstallation gameInstallation, VersionDefinition.ReadOnly versionDefinition);

    /// <summary>
    /// Suggest a version definition for a given game installation
    /// </summary>
    public Optional<VersionData> SuggestVersionDefinitions(GameInstallation gameInstallation, IEnumerable<(GamePath Path, Hash Hash)> files);

}

public record struct VersionData(string[] LocatorIds, string VersionName);
