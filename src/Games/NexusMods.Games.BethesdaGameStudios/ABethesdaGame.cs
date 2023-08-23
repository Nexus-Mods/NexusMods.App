using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.Games.GameCapabilities.FomodCustomInstallPathCapability;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.BethesdaGameStudios.Capabilities;
using NexusMods.Games.Generic.Installers;

namespace NexusMods.Games.BethesdaGameStudios;

/// <summary>
/// The base class for all Bethesda games.
/// This should contain functionality that is common to all Bethesda games.
/// </summary>
public abstract class ABethesdaGame : AGame
{
    private readonly IModInstaller[] _installers;

    /// <inheritdoc />
    protected ABethesdaGame(IEnumerable<IGameLocator> gameLocators, IServiceProvider provider) : base(gameLocators)
    {
        _installers = new IModInstaller[]
        {
            // Handles common installs to the game folder and other common directories like `Data`
            GenericFolderMatchInstaller.Create(provider, BethesdaInstallFolderTargets.InstallFolderTargets()),
        };
    }

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => _installers;
}
