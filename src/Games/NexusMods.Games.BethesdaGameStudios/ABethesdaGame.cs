using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;
using NexusMods.Games.BethesdaGameStudios.Capabilities;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// The base class for all Bethesda games.
/// This should contain functionality that is common to all Bethesda games.
/// </summary>
public abstract class ABethesdaGame : AGame
{

    /// <inheritdoc />
    protected ABethesdaGame(IEnumerable<IGameLocator> gameLocators) : base(gameLocators) { }

    /// <inheritdoc />
    public override Dictionary<GameCapabilityId, IGameCapability> SupportedCapabilities()
    {
        var capabilities = base.SupportedCapabilities();

        // Support for installing simple Data and GameRoot level mods.
        capabilities.Add(AFolderMatchInstallerCapability.CapabilityId, new BethesdaFolderMatchInstallerCapability());
        // Configure FOMOD install to install to Data folder instead of GameRoot.
        capabilities.Add(AFomodCustomInstallPathCapability.CapabilityId, new FomodDataInstallPathCapability());

        return capabilities;
    }
}
