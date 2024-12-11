using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Steam;

namespace NexusMods.Networking.Steam;

public static class Services
{
    /// <summary>
    /// Add the steam store DI systems to the container
    /// </summary>
    public static IServiceCollection AddSteamStore(this IServiceCollection services)
    {
        services.AddSingleton<ISteamSession, Session>();
        return services;
    }
    
    /// <summary>
    /// Adds a logging authentication handler to the DI container
    /// </summary>
    public static IServiceCollection AddLoggingAuthenticationHandler(this IServiceCollection services)
    {
        services.AddSingleton<IAuthInterventionHandler, LoggingAuthInterventionHandler>();
        return services;
    }
    
    /// <summary>
    /// Adds auth storage to the DI container that stores the auth data in the app directory
    /// </summary>
    public static IServiceCollection AddLocalAuthFileStorage(this IServiceCollection services)
    {
        services.AddSingleton<IAuthStorage, AppDirectoryAuthStorage>();
        return services;
    }
    
}
