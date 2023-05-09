using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Xbox;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;
using IGame = NexusMods.DataModel.Games.IGame;

namespace NexusMods.StandardGameLocators.TestHelpers;

public static class Services
{
    public static IServiceCollection AddStubbedGameLocators(this IServiceCollection coll)
    {
        coll.AddAllSingleton<IGame, StubbedGame>();
        coll.AddAllSingleton<IModInstaller, StubbedGameInstaller>();
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

        coll.AddSingleton<AHandler<SteamGame, SteamGameId>>(s =>
            new StubbedGameLocator<SteamGame, SteamGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new SteamGame(SteamGameId.From( 42), "Stubbed Game", tfm.CreateFolder("steam_game").Path),
                game => game.AppId));

        coll.AddSingleton<AHandler<XboxGame, XboxGameId>>(s =>
            new StubbedGameLocator<XboxGame, XboxGameId>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new XboxGame(XboxGameId.From("xbox-game-id"), "Stubbed Game", tfm.CreateFolder("xbox_game").Path),
                game => game.Id));
        return coll;
    }
}
