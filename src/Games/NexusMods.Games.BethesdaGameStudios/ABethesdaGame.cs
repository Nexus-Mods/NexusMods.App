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
    private readonly GameCapabilityCollection _capabilities;

    /// <inheritdoc />
    protected ABethesdaGame(IEnumerable<IGameLocator> gameLocators) : base(gameLocators)
    {
        _capabilities = base.SupportedCapabilities;
        // Support for installing simple Data and GameRoot level mods.
        _capabilities.Register(AFolderMatchInstallerCapability.CapabilityId, new BethesdaFolderMatchInstallerCapability());
        // Configure FOMOD install to install to Data folder instead of GameRoot.
        _capabilities.Register(AFomodCustomInstallPathCapability.CapabilityId, new FomodDataInstallPathCapability());
    }

    /// <inheritdoc />
    public override GameCapabilityCollection SupportedCapabilities => _capabilities;
}
