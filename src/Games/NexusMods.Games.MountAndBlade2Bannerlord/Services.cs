using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.MountAndBlade2Bannerlord.Analyzers;
using NexusMods.Games.MountAndBlade2Bannerlord.Installers;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class ServicesExtensions
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<LauncherManagerFactory>();
        services.AddSingleton<GamePathProvierFactory>();

        services.AddAllSingleton<IGame, MountAndBlade2Bannerlord>();
        services.AddSingleton<IModInstaller, MountAndBlade2BannerlordModInstaller>();
        services.AddSingleton<IFileAnalyzer, MountAndBlade2BannerlordAnalyzer>();
        services.AddSingleton<ITool, RunStandaloneTool>();
        services.AddSingleton<ITool, RunLauncherTool>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
