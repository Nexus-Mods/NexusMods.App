using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;
using NexusMods.Games.UnrealEngine.Installers;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Installers;

namespace NexusMods.Games.UnrealEngine.PacificDrive;

[UsedImplicitly]
public class PacificDriveGame : AGame, ISteamGame, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("pacificdrive");
    private readonly IFileSystem _fileSystem;
    private readonly IServiceProvider _serviceProvider;

    public PacificDriveGame(IEnumerable<IGameLocator> gameLocators, IFileSystem fileSystem, IServiceProvider provider) : base(provider)
    {
        _fileSystem = fileSystem;
        _serviceProvider = provider;
    }

    public override string Name => "Pacific Drive";
    public override GameDomain Domain => StaticDomain;
    public override GamePath GetPrimaryFile(GameStore store) => new(LocationId.Game, "PenDriverPro/Binaries/Win64/PenDriverPro-Win64-Shipping.exe");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            { Constants.GameMainUE, installation.Path.Combine("PenDriverPro")},
            {
                LocationId.Saves,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine("PenDriverPro/Saved/SaveGames")
            },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine("PenDriverPro")
            }
        };

        return result;
    }

    public IEnumerable<uint> SteamIds => [1458140u];
    public IEnumerable<string> EpicCatalogItemId => ["c75f6d17cb064f52bbf07c61df32e30f"];

    public override IStreamFactory Icon =>
        new EmbededResourceStreamFactory<PacificDriveGame>("NexusMods.Games.UnrealEngine.Resources.PacificDrive.icon.png");

    public override IStreamFactory GameImage =>
        new EmbededResourceStreamFactory<PacificDriveGame>("NexusMods.Games.UnrealEngine.Resources.PacificDrive.game_image.jpg");

    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        _serviceProvider.GetRequiredService<SmartUEInstaller>(),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
    {
        return new PacificDriveLoadoutSynchronizer(provider);
    }
}
