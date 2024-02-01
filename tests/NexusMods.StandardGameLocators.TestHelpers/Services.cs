using GameFinder.Common;
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
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.StandardGameLocators.TestHelpers;

public static class Services
{
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
                tfm => new GOGGame(GOGGameId.From(42), "Stubbed Game", tfm.CreateFolder("gog_game").Path),
                game => game.Id));

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
