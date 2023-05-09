using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EADesktop.Crypto;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Xbox;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Adds services tied to this library to your Dependency Injector container.
/// </summary>
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
        OSInformation.Shared.SwitchPlatform(
            onWindows: () =>
            {
                services.AddSingleton<IGameLocator, SteamLocator>();
                services.AddSingleton<IGameLocator, EADesktopLocator>();
                services.AddSingleton<IGameLocator, EpicLocator>();
                services.AddSingleton<IGameLocator, GogLocator>();
                services.AddSingleton<IGameLocator, OriginLocator>();
                services.AddSingleton<IGameLocator, XboxLocator>();
            },
            onLinux: () =>
            {
                services.AddSingleton<IGameLocator, SteamLocator>();
            });

        if (!registerConcreteLocators) return services;

        OSInformation.Shared.SwitchPlatform(
            onWindows: () =>
            {
                services.AddSingleton(WindowsRegistry.Shared);
                services.AddSingleton<IHardwareInfoProvider, HardwareInfoProvider>();
                services.AddSingleton<AHandler<SteamGame, SteamGameId>>(provider => new SteamHandler(provider.GetRequiredService<IFileSystem>(), provider.GetRequiredService<IRegistry>()));
                services.AddSingleton<AHandler<EADesktopGame, EADesktopGameId>>(provider => new EADesktopHandler(provider.GetRequiredService<IFileSystem>(), provider.GetRequiredService<IHardwareInfoProvider>()));
                services.AddSingleton<AHandler<EGSGame, EGSGameId>>(provider => new EGSHandler(provider.GetRequiredService<IRegistry>(), provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<GOGGame, GOGGameId>>(provider => new GOGHandler(provider.GetRequiredService<IRegistry>(), provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<OriginGame, OriginGameId>>(provider => new OriginHandler(provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<XboxGame, XboxGameId>>(provider => new XboxHandler(provider.GetRequiredService<IFileSystem>()));
            },
            onLinux: () =>
            {
                services.AddSingleton<AHandler<SteamGame, SteamGameId>>(provider => new SteamHandler(provider.GetRequiredService<IFileSystem>(), registry: null));
            });

        return services;
    }
}
