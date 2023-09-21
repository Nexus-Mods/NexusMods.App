using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic.Installers;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

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
            // Default installer for FOMODs
            FomodXmlInstaller.Create(provider, new GamePath(GameFolderType.Game, "Data".ToRelativePath())),
            // Handles common installs to the game folder and other common directories like `Data`
            GenericFolderMatchInstaller.Create(provider, BethesdaInstallFolderTargets.InstallFolderTargets()),
        };
    }

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => _installers;

    // Note: Not cached because it's not used often
    public override List<IModInstallDestination> InstallDestinations => GetInstallDestinations();

    private List<IModInstallDestination> GetInstallDestinations()
    {
        var result = new List<IModInstallDestination>();
        ModInstallDestinationHelpers.AddInstallFolderTargets(BethesdaInstallFolderTargets.InstallFolderTargets(), result);
        ModInstallDestinationHelpers.AddCommonLocations(GetLocations(), result);
        return result;
    }
}
