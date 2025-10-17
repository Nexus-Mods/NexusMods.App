using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.Generic.Dependencies;
using NexusMods.Games.Generic.FileAnalyzers;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Games.Generic;

public static class Services
{
    public static IServiceCollection AddGenericGameSupport(this IServiceCollection services)
    {
        services.AddSingleton<IniAnalysisData>();
        services.AddSingleton<GameToolRunner>();

        if (OSInformation.Shared.IsLinux)
        {
            services.AddSingleton<ProtontricksNativeDependency>();
            services.AddSingleton<ProtontricksFlatpakDependency>();
            services.AddSingleton<AggregateProtontricksDependency>();
            services.AddSingleton<IRuntimeDependency, AggregateProtontricksDependency>();
        }

        return services;
    }
}
