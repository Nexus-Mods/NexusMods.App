using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.ModInstallers;

namespace NexusMods.Games.FOMOD;

public static class Services
{
    /// <summary>
    /// Adds FOMOD [currently XML only] support to your DI container.
    /// </summary>
    /// <param name="services">The services to add.</param>
    /// <returns>Collection of services.</returns>
    public static IServiceCollection AddFOMOD(this IServiceCollection services)
    {
        services.AddAllSingleton<IModInstaller, FomodXmlInstaller>();
        return services;
    }
}
