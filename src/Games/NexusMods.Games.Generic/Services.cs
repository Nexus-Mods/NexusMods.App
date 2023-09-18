using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.Generic.FileAnalyzers;

namespace NexusMods.Games.Generic;

public static class Services
{
    public static IServiceCollection AddGenericGameSupport(this IServiceCollection services)
    {
        services.AddSingleton<IniAnalysisData>();
        return services;
    }
}
