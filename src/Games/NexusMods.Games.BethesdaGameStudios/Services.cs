using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
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
        services.AddAllSingleton<IFileAnalyzer, PluginAnalyzer>();
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
    
}