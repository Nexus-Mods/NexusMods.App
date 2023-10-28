using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Games.GameCapabilities.FolderMatchInstallerCapability;
using NexusMods.DataModel.LoadoutSynchronizer;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI;
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
    private readonly Lazy<PluginSorter> _pluginSorter;

    public PluginSorter PluginSorter => _pluginSorter.Value;

    /// <inheritdoc />
    protected ABethesdaGame(IServiceProvider provider) : base(provider)
    {
        _installers = new IModInstaller[]
        {
            // // Default installer for FOMODs
            // FomodXmlInstaller.Create(provider, new GamePath(LocationId.Game, "Data".ToRelativePath())),
            // // Handles common installs to the game folder and other common directories like `Data`
            // GenericFolderMatchInstaller.Create(provider, BethesdaInstallFolderTargets.InstallFolderTargets()),

            // Handles everything else
            AdvancedInstaller<UnsupportedModOverlayViewModelFactory, AdvancedInstallerOverlayViewModelFactory>.Create(provider),
        };

        _pluginSorter = new Lazy<PluginSorter>(provider.GetRequiredService<PluginSorter>);
    }

    /// <inheritdoc />
    public override IEnumerable<IModInstaller> Installers => _installers;

    protected override IStandardizedLoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new BethesdaLoadoutSynchronizer(provider);
    }

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
    {
        var result = new List<IModInstallDestination>();
        ModInstallDestinationHelpers.AddInstallFolderTargets(BethesdaInstallFolderTargets.InstallFolderTargets(), result);
        ModInstallDestinationHelpers.AddCommonLocations(locations, result);
        return result;
    }
}
