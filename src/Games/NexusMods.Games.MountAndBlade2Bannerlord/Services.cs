using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Extensions.DependencyInjection;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class ServicesExtensions
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.AddSingleton<LauncherManagerFactory>();

        services.AddGame<MountAndBlade2Bannerlord>()
            .AddSingleton<ILoadoutDiagnosticEmitter, BuiltInEmitter>()
            .AddSingleton<ITool, RunStandaloneTool>()
            .AddSingleton<ITool, RunLauncherTool>()
            .AddSingleton<ITypeFinder, TypeFinder>();

        return services;
    }
}
