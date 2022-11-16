using System.Runtime.InteropServices;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Interfaces.Components;

namespace NexusMods.StandardGameLocators;

public static class Services
{
    
    /// <summary>
    /// Registers all the services for the standard store locators
    /// </summary>
    /// <param name="services">Collection to append entries into</param>
    /// <param name="registerConcreteLocators">if true, will register the concrete locators, set this to false if
    /// you plan on stubbing out these locators for testing</param>
    /// <returns></returns>
    public static IServiceCollection AddStandardGameLocators(this IServiceCollection services,
        bool registerConcreteLocators = true)
    {
        services.AddSingleton<IGameLocator, SteamLocator>();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<IGameLocator, GogLocator>();
        }

        if (!registerConcreteLocators) return services;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            services.AddSingleton<AHandler<GOGGame, long>>(_ => new GOGHandler());

        services.AddSingleton(s => CreateSteamHandler());
        
        return services;
    }

    private static AHandler<SteamGame, int> CreateSteamHandler()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new SteamHandler(new WindowsRegistry())
            : new SteamHandler(null);
    }
}