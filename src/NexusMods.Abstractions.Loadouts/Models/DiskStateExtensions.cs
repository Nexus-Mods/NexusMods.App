using System.Diagnostics;
using DynamicData.Kernel;
using NexusMods.Abstractions.GameLocators;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Extensions to the GameMetadata class to handle disk state
/// </summary>
public static class DiskStateExtensions
{
    
    /// <summary>
    /// Gets the latest game metadata for the installation
    /// </summary>
    public static GameInstallMetadata.ReadOnly GetMetadata(this GameInstallation installation, IConnection connection)
    {
        return GameInstallMetadata.Load(connection.Db, installation.GameMetadataId);
    }
}
