using GameFinder.Common;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public class StubbedTestHarness<TGame> where TGame : IGame
{

}

public static class StubbedTestHarnessExtensions
{
    public static IServiceCollection AddUniversalGameLocator<TGame>(this IServiceCollection services, Version version) 
        where TGame : ISteamGame
    {
        services.AddSingleton<IGameLocator, UniversalStubbedGameLocator<TGame>>(s => 
            new UniversalStubbedGameLocator<TGame>(s.GetRequiredService<TemporaryFileManager>(),
                version));
        return services;
    }
}