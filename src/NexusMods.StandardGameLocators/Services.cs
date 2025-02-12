using System.Text.Json.Serialization;
using GameFinder.Common;
using GameFinder.Launcher.Heroic;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.EADesktop;
using GameFinder.StoreHandlers.EADesktop.Crypto;
using GameFinder.StoreHandlers.EADesktop.Crypto.Windows;
using GameFinder.StoreHandlers.EGS;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Origin;
using GameFinder.StoreHandlers.Steam;
using GameFinder.StoreHandlers.Steam.Models.ValueTypes;
using GameFinder.StoreHandlers.Xbox;
using GameFinder.Wine;
using GameFinder.Wine.Bottles;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.GameLocators.Stores.Origin;
using NexusMods.Abstractions.Settings;
using NexusMods.MnemonicDB.Abstractions;
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
    /// <param name="services"></param>
    /// <param name="registerConcreteLocators">if true, will register the concrete locators, set this to false if
    /// you plan on stubbing out these locators for testing</param>
    /// <param name="registerHeroic">if true, will register the Heroic launcher game locator</param>
    /// <param name="settings"></param>
    public static IServiceCollection AddStandardGameLocators(
        this IServiceCollection services,
        bool registerConcreteLocators = true,
        bool registerHeroic = true,
        bool registerWine = true,
        GameLocatorSettings? settings = null)
    {
        services
            .AddSettings<GameLocatorSettings>()
            .AddGameLocatorCliVerbs()
            .AddAttributeCollection(typeof(ManuallyAddedGame));

        // TODO: figure out the Proton-Wine situation

        services.AddSingleton<IGameLocator, ManuallyAddedLocator>();

        OSInformation.Shared.SwitchPlatform(
            onWindows: () =>
            {
                services.AddSingleton<IGameLocator, SteamLocator>();
                services.AddSingleton<IGameLocator, EADesktopLocator>();
                services.AddSingleton<IGameLocator, EpicLocator>();
                services.AddSingleton<IGameLocator, GogLocator>();
                services.AddSingleton<IGameLocator, OriginLocator>();

                if (settings is not null && settings.EnableXboxGamePass) services.AddSingleton<IGameLocator, XboxLocator>();
            },
            onLinux: () =>
            {
                services.AddSingleton<IGameLocator, SteamLocator>();
                if (registerHeroic) 
                    services.AddSingleton<IGameLocator, HeroicGogLocator>();

                if (registerWine)
                {
                    services.AddSingleton<IGameLocator, DefaultWineGameLocator>();
                    services.AddSingleton<IGameLocator, BottlesWineGameLocator>();
                }
            },
            onOSX: () =>
            {
                services.AddSingleton<IGameLocator, SteamLocator>();
            });


        services.AddSingleton<JsonConverter, GameInstallationConverter>();

        if (!registerConcreteLocators) return services;

        OSInformation.Shared.SwitchPlatform(
            onWindows: () =>
            {
#pragma warning disable CA1416
                services.AddSingleton(WindowsRegistry.Shared);
                services.AddSingleton<IHardwareInfoProvider, HardwareInfoProvider>();
#pragma warning restore CA1416
                services.AddSingleton<AHandler<SteamGame, AppId>>(provider => new SteamHandler(provider.GetRequiredService<IFileSystem>(), provider.GetRequiredService<IRegistry>()));
                services.AddSingleton<AHandler<EADesktopGame, EADesktopGameId>>(provider => new EADesktopHandler(provider.GetRequiredService<IFileSystem>(), provider.GetRequiredService<IHardwareInfoProvider>()));
                services.AddSingleton<AHandler<EGSGame, EGSGameId>>(provider => new EGSHandler(provider.GetRequiredService<IRegistry>(), provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<GOGGame, GOGGameId>>(provider => new GOGHandler(provider.GetRequiredService<IRegistry>(), provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<OriginGame, OriginGameId>>(provider => new OriginHandler(provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<AHandler<XboxGame, XboxGameId>>(provider => new XboxHandler(provider.GetRequiredService<IFileSystem>()));
            },
            onLinux: () =>
            {
                services.AddSingleton<AHandler<SteamGame, AppId>>(provider => new SteamHandler(provider.GetRequiredService<IFileSystem>(), registry: null));
                services.AddSingleton<HeroicGOGHandler>(provider => new HeroicGOGHandler(provider.GetRequiredService<IFileSystem>()));

                if (registerWine)
                {
                    services.AddSingleton<IWinePrefixManager<WinePrefix>>(provider => new DefaultWinePrefixManager(provider.GetRequiredService<IFileSystem>()));
                    services.AddSingleton<IWinePrefixManager<BottlesWinePrefix>>(provider => new BottlesWinePrefixManager(provider.GetRequiredService<IFileSystem>()));

                    services.AddSingleton(provider => new WineStoreHandlerWrapper(
                            provider.GetRequiredService<IFileSystem>(),
                            [
                                (_, wineRegistry, wineFileSystem) => new GOGHandler(wineRegistry, wineFileSystem),
                                (_, wineRegistry, wineFileSystem) => new EGSHandler(wineRegistry, wineFileSystem),
                                (_, _, wineFileSystem) => new OriginHandler(wineFileSystem),
                            ],
                            [
                                CreateDelegateFor<GOGGame, IGogGame>(
                                    (foundGame, requestedGame) => requestedGame.GogIds.Any(x => foundGame.Id.Equals(x)),
                                    game => new GameLocatorResult(game.Path, game.Path.FileSystem, GameStore.GOG,
                                        GogLocator.CreateMetadataCore(game)
                                    )
                                ),
                                CreateDelegateFor<EGSGame, IEpicGame>(
                                    (foundGame, requestedGame) => requestedGame.EpicCatalogItemId.Any(x => foundGame.CatalogItemId.Equals(x)),
                                    game => new GameLocatorResult(game.InstallLocation, game.InstallLocation.FileSystem, GameStore.EGS,
                                        EpicLocator.CreateMetadataCore(game)
                                    )
                                ),
                                CreateDelegateFor<OriginGame, IOriginGame>(
                                    (foundGame, requestedGame) => requestedGame.OriginGameIds.Any(x => foundGame.Id.Equals(x)),
                                    game => new GameLocatorResult(game.InstallPath, game.InstallPath.FileSystem, GameStore.Origin,
                                        OriginLocator.CreateMetadataCore(game)
                                    )
                                ),
                            ]
                        )
                    );
                }
            },
        onOSX: () =>
            {
                services.AddSingleton<AHandler<SteamGame, AppId>>(provider => new SteamHandler(provider.GetRequiredService<IFileSystem>(), registry: null));
            });

        return services;
    }

    private static WineStoreHandlerWrapper.Matches CreateDelegateFor<TFoundGame, TRequestedGame>(
        Func<TFoundGame, TRequestedGame, bool> matches,
        Func<TFoundGame, GameLocatorResult> createResult)
        where TFoundGame : GameFinder.Common.IGame
        where TRequestedGame : ILocatableGame
    {
        return (foundGame, requestedGame) =>
        {
            if (foundGame is not TFoundGame typedFoundGame) return null;
            if (requestedGame is not TRequestedGame typedRequestedGame) return null;
            if (!matches(typedFoundGame, typedRequestedGame)) return null;
            return createResult(typedFoundGame);
        };
    }
}
