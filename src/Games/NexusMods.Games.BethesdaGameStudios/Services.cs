using Microsoft.Extensions.DependencyInjection;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services 
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, SkyrimSpecialEdition>();
        return services;
    }
    
}