using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public static class Services
{
    public static IServiceCollection AddStubbedGameLocators(this IServiceCollection coll)
    {
        
        coll.AddAllSingleton<IGame, StubbedGame>();
        coll.AddSingleton<AHandler<EADesktopGame, string>>(s => 
            new StubbedGameLocator<EADesktopGame, string>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new EADesktopGame("ea-game-id", "Stubbed Game", tfm.CreateFolder("ea_game").Path.ToString()),
                game => game.SoftwareID));
        
        coll.AddSingleton<AHandler<EGSGame, string>>(s => 
            new StubbedGameLocator<EGSGame, string>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new EGSGame("epic-game-id", "Stubbed Game", tfm.CreateFolder("epic_game").Path.ToString()),
                game => game.CatalogItemId));
        
        coll.AddSingleton<AHandler<OriginGame, string>>(s => 
            new StubbedGameLocator<OriginGame, string>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new OriginGame("origin-game-id", tfm.CreateFolder("origin_game").Path.ToString()),
                game => game.Id));

        coll.AddSingleton<AHandler<GOGGame, long>>(s => 
            new StubbedGameLocator<GOGGame, long>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new GOGGame(42, "Stubbed Game", tfm.CreateFolder("gog_game").Path.ToString()),
                game => game.Id));
        
        coll.AddSingleton<AHandler<SteamGame, int>>(s => 
            new StubbedGameLocator<SteamGame, int>(s.GetRequiredService<TemporaryFileManager>(),
                tfm => new SteamGame(42, "Stubbed Game", tfm.CreateFolder("steam_game").Path.ToString()),
                game => game.AppId));
        return coll;
    }
}