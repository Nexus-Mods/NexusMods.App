using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Games.FileHashes.Emitters;
using NexusMods.Games.FOMOD;
using NexusMods.Games.RedEngine.Cyberpunk2077.Emitters;
using NexusMods.Games.RedEngine.Cyberpunk2077.SortOrder;
using NexusMods.Games.RedEngine.ModInstallers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.IO;
using NexusMods.Sdk.NexusModsApi;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

[UsedImplicitly]
public class Cyberpunk2077Game : AGame, ISteamGame, IGogGame //, IEpicGame
{
    public static readonly GameDomain StaticDomain = GameDomain.From("cyberpunk2077");
    public static GameId GameIdStatic => GameId.From(3333);
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private ISortOrderVariety[] _sortOrderVarieties = [];

    public Cyberpunk2077Game(IServiceProvider provider, IConnection connection) : base(provider)
    {
        _serviceProvider = provider;
        _connection = connection;

        _sortOrderVarieties =
        [
            provider.GetRequiredService<RedModSortOrderVariety>(),
        ];
    }

    protected override ILoadoutSynchronizer MakeSynchronizer(IServiceProvider provider)
        => new Cyberpunk2077Synchronizer(provider);

    public override string DisplayName => "Cyberpunk 2077";
    public override GameId NexusModsGameId => GameIdStatic;

    public override GamePath GetPrimaryFile(GameTargetInfo targetInfo) => new(LocationId.Game, "bin/x64/Cyberpunk2077.exe");
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            { LocationId.Game, installation.Path },
            // Skip managing saves for now, to prevent accidental deletion of saves
            // e.g. when removing loadouts, un-managing the game, or uninstalling the app
            // {
            //     LocationId.Saves,
            //     fileSystem.GetKnownPath(KnownPath.HomeDirectory).Combine("Saved Games/CD Projekt Red/Cyberpunk 2077")
            // },
            {
                LocationId.AppData,
                fileSystem.GetKnownPath(KnownPath.LocalApplicationDataDirectory)
                    .Combine("CD Projekt Red/Cyberpunk 2077")
            }
        };

        return result;
    }

    public IEnumerable<uint> SteamIds => new[] { 1091500u };
    public IEnumerable<long> GogIds => new[] { 2093619782L, 1423049311 };

    // The Epic Games Store is not supported yet, managing the game will put the user into a state where they cannot apply a loadout. 
    public IEnumerable<string> EpicCatalogItemId => new[] { "5beededaad9743df90e8f07d92df153f" };

    public override IStreamFactory Icon =>
        new EmbeddedResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.thumbnail.webp");

    public override IStreamFactory GameImage =>
        new EmbeddedResourceStreamFactory<Cyberpunk2077Game>("NexusMods.Games.RedEngine.Resources.Cyberpunk2077.tile.webp");
    
    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        new NoWayToSourceFilesOnDisk(),
        new UndeployableLoadoutDueToMissingGameFiles(_serviceProvider),
        new PatternBasedDependencyEmitter(PatternDefinitions.Definitions, _serviceProvider),
        new MissingProtontricksForRedModEmitter(_serviceProvider),
        new MissingRedModEmitter(),
        new WinePrefixRequirementsEmitter(),
    ];

    /// <inheritdoc />
    protected override ISortOrderVariety[] GetSortOrderVarieties()
    {
        return _sortOrderVarieties;
    }
    
    /// <inheritdoc />
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
        FomodXmlInstaller.Create(_serviceProvider, new GamePath(LocationId.Game, "")),
        new RedModInstaller(_serviceProvider),
        new SimpleOverlayModInstaller(_serviceProvider),
        new AppearancePresetInstaller(_serviceProvider),
        new FolderlessModInstaller(_serviceProvider),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
