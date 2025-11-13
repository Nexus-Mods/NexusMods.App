using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;
using NexusMods.Sdk.Games;

namespace NexusMods.StandardGameLocators.TestHelpers;

public static class StubbedTestHarnessExtensions
{
    public static IServiceCollection AddUniversalGameLocator<TGame>(
        this IServiceCollection services,
        Version version,
        Dictionary<RelativePath, byte[]>? gameFiles = null)
        where TGame : IGame
    {
        services
            .AddSingleton<IGameLocator, UniversalStubbedGameLocator<TGame>>(s =>
                new UniversalStubbedGameLocator<TGame>(
                    s,
                    s.GetRequiredService<IFileSystem>(),
                    s.GetRequiredService<TemporaryFileManager>(),
                    gameFiles));

        return services;
    }
}
