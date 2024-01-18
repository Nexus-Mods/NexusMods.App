using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.FOMOD.CoreDelegates;

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
        services.AddAllSingleton<ITypeFinder, TypeFinder>();
        services.AddAllSingleton<ICoreDelegates, InstallerDelegates>();
        return services;
    }
}
