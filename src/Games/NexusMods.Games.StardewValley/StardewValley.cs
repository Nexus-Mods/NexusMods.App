using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.StreamFactories;
using NexusMods.Games.StardewValley.Installers;
using NexusMods.Paths;

namespace NexusMods.Games.StardewValley;

[UsedImplicitly]
public class StardewValley : AGame, ISteamGame, IGogGame, IXboxGame
{
    private readonly IOSInformation _osInformation;
    private readonly IServiceProvider _serviceProvider;
    public IEnumerable<uint> SteamIds => new[] { 413150u };
    public IEnumerable<long> GogIds => new long[] { 1453375253 };
    public IEnumerable<string> XboxIds => new[] { "ConcernedApe.StardewValleyPC" };

    public override string Name => "Stardew Valley";

    public static GameDomain GameDomain => GameDomain.From("stardewvalley");
    public override GameDomain Domain => GameDomain;

    public StardewValley(
        IOSInformation osInformation,
        IEnumerable<IGameLocator> gameLocators,
        IServiceProvider provider) : base(provider)
    {
        _osInformation = osInformation;
        _serviceProvider = provider;
    }

    public override GamePath GetPrimaryFile(GameStore store)
    {
        // "StardewValley" is a wrapper shell script that launches the "Stardew Valley" binary
        // on OSX, it's used to check the .NET version, on Linux it just launches the game
        // for XboxGamePass, the original exe gets replaced during installation
        return _osInformation.MatchPlatform(
            state: ref store,
            onWindows: (ref GameStore gameStore) =>
                gameStore == GameStore.XboxGamePass
                    ? new GamePath(LocationId.Game, "Stardew Valley.exe")
                    : new GamePath(LocationId.Game, "StardewModdingAPI.exe"),
            onLinux: (ref GameStore _) => new GamePath(LocationId.Game, "StardewValley"),
            onOSX: (ref GameStore _) => new GamePath(LocationId.Game, "Contents/MacOS/StardewValley")
        );
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem,
        GameLocatorResult installation)
    {
        // global data files (https://github.com/Pathoschild/SMAPI/blob/8d600e226960a81636137d9bf286c69ab39066ed/src/SMAPI/Framework/ModHelpers/DataHelper.cs#L163-L169)
        var stardewValleyAppDataPath = fileSystem
            .GetKnownPath(KnownPath.ApplicationDataDirectory)
            .Combine("StardewValley");

        var result = new Dictionary<LocationId, AbsolutePath>()
        {
            {
                LocationId.Game,
                installation.Store == GameStore.XboxGamePass ? installation.Path.Combine("Content") : installation.Path
            },
            { LocationId.AppData, stardewValleyAppDataPath.Combine(".smapi") },
            { LocationId.Saves, stardewValleyAppDataPath.Combine("Saves") },
        };
        return result;
    }

    public override IStreamFactory Icon => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.icon.png");

    public override IStreamFactory GameImage => new EmbededResourceStreamFactory<StardewValley>("NexusMods.Games.StardewValley.Resources.game_image.jpg");

    public override IEnumerable<IModInstaller> Installers => new IModInstaller[]
    {
        SMAPIInstaller.Create(_serviceProvider),
        SMAPIModInstaller.Create(_serviceProvider)
    };

    public override List<IModInstallDestination> GetInstallDestinations(
        IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
}
