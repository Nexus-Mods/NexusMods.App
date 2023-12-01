using Bannerlord.LauncherManager.External;
using Bannerlord.LauncherManager.External.UI;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.MountAndBlade2Bannerlord.LauncherManager.Providers;
using NexusMods.Games.MountAndBlade2Bannerlord.Options;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;

namespace NexusMods.Games.MountAndBlade2Bannerlord;

public static class ServicesExtensions
{
    public static IServiceCollection AddMountAndBladeBannerlord(this IServiceCollection services)
    {
        services.Configure<MountAndBlade2BannerlordOptions>(opt => { });

        services.AddAllSingleton<IGame, MountAndBlade2Bannerlord>()

            .AddSingleton<UserDataProvider>()

            .AddSingleton<IFileSystemProvider, FileSystemProvider>()
            .AddScoped<IDialogProvider, DialogProvider>()
            .AddScoped<INotificationProvider, NotificationProvider>()
            .AddScoped<ILoadOrderPersistenceProvider, LoadOrderPersistenceProvider>()
            .AddScoped<ILoadOrderStateProvider, LoadOrderStateProvider>()
            .AddScoped<LauncherStateProvider>()
            .AddScoped<ILauncherStateProvider>(sp => sp.GetRequiredService<LauncherStateProvider>())
            .AddScoped<IGameInfoProvider, GameInfoProvider>()
            .AddScoped<LauncherManagerNexusMods>()

            .AddSingleton<ILoadoutDiagnosticEmitter, BuiltInEmitter>()
            .AddSingleton<ITool, RunStandaloneTool>()
            .AddSingleton<ITool, RunLauncherTool>()
            .AddSingleton<ITypeFinder, TypeFinder>();

        return services;
    }
}
