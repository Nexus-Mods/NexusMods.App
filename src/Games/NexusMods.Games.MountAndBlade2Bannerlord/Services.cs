using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class ServicesExtensions
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<LauncherManagerFactory>();

        services.AddAllSingleton<IGame, MountAndBlade2Bannerlord>()
            .AddSingleton<ILoadoutDiagnosticEmitter, BuiltInEmitter>()
            .AddSingleton<ITool, RunStandaloneTool>()
            .AddSingleton<ITool, RunLauncherTool>()
            .AddSingleton<ITypeFinder, TypeFinder>();

        return services;
    }
}
