using GameFinder.Common;
using GameFinder.Launcher.Heroic;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Xbox;
using GameFinder.Wine;
using GameFinder.Wine.Bottles;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.StardewValley;
using NexusMods.Hashing.xxHash3;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.StandardGameLocators.TestHelpers;


public static class Services
{
    /// <summary>
    /// Add a stubbed version of Stardew Valley to the service collection
    /// </summary>
    /// <param name="coll"></param>
    /// <returns></returns>
    public static IServiceCollection AddStubbedStardewValley(this IServiceCollection coll)
    {
        return coll.AddStubbedSteamGameLocator<StardewValley>("Stardew Valley", 413150, 413151, 4278718763097142923)
            .AddStubbedSteamGOGLocator<StardewValley>("Stardew Valley", 1453375253, 58211152353130355);
    }

    /// <summary>
    /// Add a stubbed version of a game to the service collection, with the given locator data
    /// </summary>
    public static IServiceCollection AddStubbedSteamGOGLocator<TGame>(this IServiceCollection coll, string name, uint gogGameId, ulong buildId)
    {
        coll.AddSingleton<AHandler<GOGGame, GOGGameId>>(s =>
            new StubbedGameLocator<GOGGame, GOGGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new GOGGame(GOGGameId.From(gogGameId), name, tfm.CreateFolder("gog_game").Path, buildId.ToString()),
                game => game.Id));
        return coll;
    }

    /// <summary>
    /// Add a stubbed version of a game to the service collection, with the given locator data
    /// </summary>
    public static IServiceCollection AddStubbedSteamGameLocator<TGame>(this IServiceCollection coll, string name, uint appId, uint depotId, ulong manifestId)
    {
        coll.AddSingleton<AHandler<SteamGame, AppId>>(s =>
            new StubbedGameLocator<SteamGame, AppId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm =>
                {
                    var gamePath = tfm.CreateFolder($"steam_folder/SteamApps/common/{name}").Path;
                    return new SteamGame
                    {
                        SteamPath = gamePath.Parent.Parent.Parent,
                        AppManifest = new AppManifest
                        {
                            AppId = AppId.From(appId),
                            Name = name,
                            InstallationDirectory = gamePath,
                            StateFlags = StateFlags.FullyInstalled,
                            ManifestPath = gamePath.Parent.Parent.Combine($"{name}.acf"),
                            InstalledDepots = new Dictionary<DepotId, InstalledDepot>
                            {
                                {
                                    DepotId.From(depotId),
                                    new InstalledDepot
                                    {
                                        DepotId = DepotId.From(depotId),
                                        SizeOnDisk = Size.GB * 2,
                                        ManifestId = ManifestId.From(manifestId),
                                    }
                                },
                            },
                        },
                        LibraryFolder = new LibraryFolder
                        {
                            Path = gamePath.Parent.Parent,
                        },
                    };
                },
                game => game.AppId));
        return coll;
    }
    
    public static IServiceCollection AddStubbedGameLocators(this IServiceCollection coll)
    {
        coll.AddGame<StubbedGame>();
        coll.AddAllSingleton<ITool, ListFilesTool>();
        coll.AddSingleton<AHandler<EADesktopGame, EADesktopGameId>>(s =>
            new StubbedGameLocator<EADesktopGame, EADesktopGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new EADesktopGame(EADesktopGameId.From("ea-game-id"), "Stubbed Game", tfm.CreateFolder("ea_game").Path),
                game => game.EADesktopGameId));

        coll.AddSingleton<AHandler<EGSGame, EGSGameId>>(s =>
            new StubbedGameLocator<EGSGame, EGSGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new EGSGame(EGSGameId.From("epic-game-id"), "Stubbed Game", tfm.CreateFolder("epic_game").Path),
                game => game.CatalogItemId));

        coll.AddSingleton<AHandler<OriginGame, OriginGameId>>(s =>
            new StubbedGameLocator<OriginGame, OriginGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new OriginGame(OriginGameId.From("origin-game-id"), tfm.CreateFolder("origin_game").Path),
                game => game.Id));

        coll.AddSingleton<AHandler<GOGGame, GOGGameId>>(s =>
            new StubbedGameLocator<GOGGame, GOGGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new GOGGame(GOGGameId.From(42), "Stubbed Game", tfm.CreateFolder("gog_game").Path, "4242"),
                game => game.Id));

        var steamId = "StubbedGameState.zip".xxHash3AsUtf8().Value;
        coll.AddSingleton<AHandler<SteamGame, AppId>>(s =>
            new StubbedGameLocator<SteamGame, AppId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm =>
                {
                    var gamePath = tfm.CreateFolder("steam_folder/SteamApps/common/steam_game").Path;
                    return new SteamGame
                    {
                        SteamPath = gamePath.Parent.Parent.Parent,
                        AppManifest = new AppManifest
                        {
                            AppId = AppId.From(42),
                            Name = "Stubbed Game",
                            InstallationDirectory = gamePath,
                            StateFlags = StateFlags.FullyInstalled,
                            ManifestPath = gamePath.Parent.Parent.Combine("steam_game.acf"),
                            InstalledDepots = new Dictionary<DepotId, InstalledDepot>()
                            {
                                {
                                    DepotId.From(uint.CreateTruncating(steamId)),
                                    new InstalledDepot
                                    {
                                        DepotId = DepotId.From(uint.CreateTruncating(steamId)),
                                        SizeOnDisk = Size.GB * 2,
                                        ManifestId = ManifestId.From(uint.CreateTruncating(steamId)),
                                    }
                                }
                            }
                        },
                        LibraryFolder = new LibraryFolder
                        {
                            Path = gamePath.Parent.Parent,
                        },
                    };
                },
                game => game.AppId));

        coll.AddSingleton<AHandler<XboxGame, XboxGameId>>(s =>
            new StubbedGameLocator<XboxGame, XboxGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new XboxGame(XboxGameId.From("xbox-game-id"), "Stubbed Game", tfm.CreateFolder("xbox_game").Path),
                game => game.Id));

        if (OSInformation.Shared.IsLinux)
        {
            coll.AddSingleton<HeroicGOGHandler>(s => new HeroicGOGHandler(s.GetRequiredService<IFileSystem>()));

            coll.AddSingleton<IWinePrefixManager<WinePrefix>>(s =>
                new StubbedWinePrefixManager<WinePrefix>(s.GetRequiredService<TemporaryFileManager>(),
                    tfm => new WinePrefix
                    {
                        ConfigurationDirectory = tfm.CreateFolder("wine-prefix").Path,
                    }));

            coll.AddSingleton<IWinePrefixManager<BottlesWinePrefix>>(s =>
                new StubbedWinePrefixManager<BottlesWinePrefix>(s.GetRequiredService<TemporaryFileManager>(),
                    tfm => new BottlesWinePrefix
                    {
                        ConfigurationDirectory = tfm.CreateFolder("bottles-prefix").Path,
                    }));

            coll.AddSingleton<WineStoreHandlerWrapper>(s => new WineStoreHandlerWrapper(
                s.GetRequiredService<IFileSystem>(),
                new WineStoreHandlerWrapper.CreateHandler[] { },
                new WineStoreHandlerWrapper.Matches[] { }));
        }

        return coll;
    }
}
