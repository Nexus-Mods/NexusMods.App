using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.DarkestDungeon.Analyzers;

namespace NexusMods.Games.DarkestDungeon;

public static class Services
{
    public static IServiceCollection AddDarkestDungeon(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IGame, DarkestDungeon>();
        serviceCollection.AddSingleton<IFileAnalyzer, ProjectAnalyzer>();
        serviceCollection.AddSingleton<IModInstaller, DarkestDungeonModInstaller>();
        serviceCollection.AddSingleton<ITypeFinder, TypeFinder>();
        return serviceCollection;
    }

}
