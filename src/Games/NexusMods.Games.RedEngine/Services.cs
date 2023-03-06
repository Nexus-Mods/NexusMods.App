using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.RedEngine.FileAnalyzers;
using NexusMods.Games.RedEngine.ModInstallers;

namespace NexusMods.Games.RedEngine;

public static class Services
{
    public static IServiceCollection AddRedEngineGames(this IServiceCollection services)
    {
        services.AddAllSingleton<IGame, Cyberpunk2077>();
        services.AddSingleton<IModInstaller, SimpleOverlayModInstaller>();
        services.AddSingleton<IModInstaller, FolderlessModInstaller>();
        services.AddSingleton<IModInstaller, AppearancePreset>();
        services.AddSingleton<IModInstaller, RedModInstaller>();
        services.AddSingleton<IFileAnalyzer, RedModInfoAnalyzer>();
        services.AddSingleton<ITool, RunGameTool<Cyberpunk2077>>();
        services.AddSingleton<ITool, RedModDeployTool>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
