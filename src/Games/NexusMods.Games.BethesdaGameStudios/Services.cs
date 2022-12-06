using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.BethesdaGameStudios.Installers;
using NexusMods.Interfaces;
using NexusMods.Interfaces.Components;

namespace NexusMods.Games.BethesdaGameStudios;

public static class Services 
{
    public static IServiceCollection AddBethesdaGameStudios(this IServiceCollection services)
    {
        services.AddAllSingleton<IModInstaller, LooseFileInstaller>();
        services.AddAllSingleton<IGame, SkyrimSpecialEdition>();
        return services;
    }
    
}