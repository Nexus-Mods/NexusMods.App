using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.MountAndBlade2Bannerlord.Emitters;
using NexusMods.Games.MountAndBlade2Bannerlord.Services;
using NexusMods.MnemonicDB.Abstractions;

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
            .AddAttributeCollection(typeof(MnemonicDB.ModuleInfoExtended))
            .AddAttributeCollection(typeof(MnemonicDB.DependentModule))
            .AddAttributeCollection(typeof(MnemonicDB.SubModuleInfo))
            .AddAttributeCollection(typeof(MnemonicDB.SubModuleFileMetadata))
            .AddAttributeCollection(typeof(MnemonicDB.ModuleFileMetadata))
            .AddAttributeCollection(typeof(MnemonicDB.DependentModuleMetadata));

        return services;
    }
}
