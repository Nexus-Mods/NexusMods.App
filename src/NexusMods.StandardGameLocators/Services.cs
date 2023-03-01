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
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

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

        MaybeAddSteamHandler(services, registerConcreteLocators);

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
        
        return services;
    }

    private static void MaybeAddSteamHandler(IServiceCollection services, 
        bool registerConcreteLocators = true)
    {
        services.AddSingleton<IGameLocator, SteamLocator>();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#pragma warning disable CA1416
            if (registerConcreteLocators) 
                services.AddSingleton<AHandler<SteamGame, int>, SteamHandler>(_ => new SteamHandler(new WindowsRegistry()));
#pragma warning restore CA1416
            return;
        }

        var searchPaths = new[]
        {
            KnownFolders.HomeFolder.CombineUnchecked(".steam/debian-installation/".ToRelativePath()),
            //KnownFolders.HomeFolder.CombineUnchecked(".steam/steam/".ToRelativePath())
        };
        
        var steamPath = searchPaths.FirstOrDefault(p => p.CombineChecked("steam.sh".ToRelativePath()).FileExists);
        if (steamPath == default) return;
        

        
        if (registerConcreteLocators) 
            services.AddSingleton<AHandler<SteamGame, int>, SteamHandler>(_ => 
                new SteamHandler(steamPath.ToString(), new FileSystem(), null));
    }
}