using System.IO.Abstractions;
using System.Runtime.InteropServices;
using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

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
            services.AddSingleton<IGameLocator, EADesktopLocator>();
            services.AddSingleton<IGameLocator, EpicLocator>();
            services.AddSingleton<IGameLocator, GogLocator>();
            services.AddSingleton<IGameLocator, OriginLocator>();
        }

        if (!registerConcreteLocators) return services;

#pragma warning disable CA1416
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddSingleton<AHandler<EADesktopGame, string>>(_ => new EADesktopHandler());
            services.AddSingleton<AHandler<EGSGame, string>>(_ => new EGSHandler());
            services.AddSingleton<AHandler<GOGGame, long>>(_ => new GOGHandler());
            services.AddSingleton<AHandler<OriginGame, string>>(_ => new OriginHandler());
        }
#pragma warning restore CA1416

        services.AddSingleton(s => CreateSteamHandler());
        
        return services;
    }

    public static IServiceCollection AddSteamGameLocator(this IServiceCollection services,
        bool registerConcreteLocator = true)
    {
        services.AddSingleton<IGameLocator, SteamLocator>();
        if (registerConcreteLocator)
            services.AddSingleton(s => CreateSteamHandler());
        return services;
    }

    private static AHandler<SteamGame, int> CreateSteamHandler()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new SteamHandler(new WindowsRegistry());

        var searchPaths = new[]
        {
            KnownFolders.HomeFolder.Join(".steam/debian-installation/".ToRelativePath())
        };


        var steamPath = searchPaths.First(p => p.Join("steam.sh".ToRelativePath()).FileExists);

        return new SteamHandler(steamPath.ToString(), new FileSystem(), null);
    }
}