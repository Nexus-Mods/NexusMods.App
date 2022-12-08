using Microsoft.Extensions.DependencyInjection;
using NexusMods.Interfaces.Components;

namespace NexusMods.Games.DarkestDungeon;

public static class Services 
{
    public static IServiceCollection AddDarkestDungeon(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGame, DarkestDungeon>();
        return serviceCollection;
    }
    
}