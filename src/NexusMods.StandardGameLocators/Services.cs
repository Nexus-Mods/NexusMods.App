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
using GameFinder.Wine;
using GameFinder.Wine.Bottles;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
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
        // TODO: figure out the Proton-Wine situation

        services.AddSingleton<IGameLocator, ManuallyAddedLocator>();
        services.AddSingleton<ITypeFinder, TypeFinder>();

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

                services.AddSingleton<IGameLocator, DefaultWineGameLocator>();
                services.AddSingleton<IGameLocator, BottlesWineGameLocator>();
            });

        if (!registerConcreteLocators) return services;

        OSInformation.Shared.SwitchPlatform(
            onWindows: () =>
            {
#pragma warning disable CA1416
                services.AddSingleton(WindowsRegistry.Shared);
                services.AddSingleton<IHardwareInfoProvider, HardwareInfoProvider>();
#pragma warning restore CA1416
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

                services.AddSingleton<IWinePrefixManager<WinePrefix>>(provider => new DefaultWinePrefixManager(provider.GetRequiredService<IFileSystem>()));
                services.AddSingleton<IWinePrefixManager<BottlesWinePrefix>>(provider => new BottlesWinePrefixManager(provider.GetRequiredService<IFileSystem>()));

                services.AddSingleton(provider => new WineStoreHandlerWrapper(
                    provider.GetRequiredService<IFileSystem>(),
                    new WineStoreHandlerWrapper.CreateHandler[]
                    {
                        (_, wineRegistry, wineFileSystem) => new GOGHandler(wineRegistry, wineFileSystem),
                        (_, wineRegistry, wineFileSystem) => new EGSHandler(wineRegistry, wineFileSystem),
                        (_, _, wineFileSystem) => new OriginHandler(wineFileSystem),
                    },
                    new[]
                    {
                        CreateDelegateFor<GOGGame, IGogGame>(
                            (foundGame, requestedGame) => requestedGame.GogIds.Any(x => foundGame.Id.Equals(x)),
                            game => new GameLocatorResult(game.Path, GameStore.GOG, GogLocator.CreateMetadataCore(game))
                        ),
                        CreateDelegateFor<EGSGame, IEpicGame>(
                            (foundGame, requestedGame) => requestedGame.EpicCatalogItemId.Any(x => foundGame.CatalogItemId.Equals(x)),
                            game => new GameLocatorResult(game.InstallLocation, GameStore.EGS, EpicLocator.CreateMetadataCore(game))
                        ),
                        CreateDelegateFor<OriginGame, IOriginGame>(
                            (foundGame, requestedGame) => requestedGame.OriginGameIds.Any(x => foundGame.Id.Equals(x)),
                            game => new GameLocatorResult(game.InstallPath, GameStore.Origin, OriginLocator.CreateMetadataCore(game))
                        ),
                    }
                ));
            });

        return services;
    }

    private static WineStoreHandlerWrapper.Matches CreateDelegateFor<TFoundGame, TRequestedGame>(
        Func<TFoundGame, TRequestedGame, bool> matches,
        Func<TFoundGame, GameLocatorResult> createResult)
        where TFoundGame : GameFinder.Common.IGame
        where TRequestedGame : DataModel.Games.IGame
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
