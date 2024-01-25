using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Stores.Steam;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators.TestHelpers;

public static class StubbedTestHarnessExtensions
{
    public static IServiceCollection AddUniversalGameLocator<TGame>(
        this IServiceCollection services,
        Version version,
        Dictionary<RelativePath, byte[]>? gameFiles = null)
        where TGame : ISteamGame
    {
        services
            .AddSingleton<IGameLocator, UniversalStubbedGameLocator<TGame>>(s =>
                new UniversalStubbedGameLocator<TGame>(
                    s.GetRequiredService<IFileSystem>(),
                    s.GetRequiredService<TemporaryFileManager>(),
                    version,
                    gameFiles));

        return services;
    }
}
