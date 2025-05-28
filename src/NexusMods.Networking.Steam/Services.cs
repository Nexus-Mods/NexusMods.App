using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.Models;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Networking.Steam.CLI;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Networking.Steam;

public static class Services
{
    /// <summary>
    /// Add the steam store DI systems to the container
    /// </summary>
    public static IServiceCollection AddSteam(this IServiceCollection services)
    {
        services.AddSingleton<ISteamSession, Session>();
        services.AddSteamLicensesModel();
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
    
    public static IServiceCollection AddSteamCli(this IServiceCollection services)
    {
        services.AddOptionParser<AppId>(s =>
            {
                if (uint.TryParse(s, out var parsed))
                    return (AppId.From(parsed), null);
                return (default(AppId), "Invalid AppId");
            }
        );
        services.AddSteam();
        services.AddSingleton<IAuthInterventionHandler, RenderingAuthenticationHandler>();
        services.AddLocalAuthFileStorage();
        services.AddSteamVerbs();
        services.AddSingleton<IReadOnlyFileStore, ReadOnlyFileStore>();
        return services;
    }
    
}
