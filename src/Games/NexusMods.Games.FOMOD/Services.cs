using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.FOMOD;

public static class Services
{
    /// <summary>
    /// Adds FOMOD [currently XML only] support to your DI container.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>Collection of services.</returns>
    public static IServiceCollection AddFomod(this IServiceCollection services)
    {
        services.AddAllSingleton<IFileAnalyzer, FomodAnalyzer>();
        services.AddAllSingleton<IModInstaller, FomodXmlInstaller>();
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
