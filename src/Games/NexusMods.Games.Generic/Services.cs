using Microsoft.Extensions.DependencyInjection;
using NexusMods.CrossPlatform.Process;
using NexusMods.Games.Generic.FileAnalyzers;

namespace NexusMods.Games.Generic;

public static class Services
{
    public static IServiceCollection AddGenericGameSupport(this IServiceCollection services)
    {
        services.AddSingleton<IniAnalysisData>();
        services.AddSingleton<GameToolRunner>();
        return services;
    }
}
