using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.DarkestDungeon;

public static class Services 
{
    public static IServiceCollection AddDarkestDungeon(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGame, DarkestDungeon>();
        serviceCollection.AddSingleton<IModInstaller, DarkestDungeonModInstaller>();
        return serviceCollection;
    }
    
}