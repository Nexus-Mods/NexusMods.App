using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.Generic.FileAnalyzers;
using NexusMods.Games.Generic.IntrinsicFiles.Models;

namespace NexusMods.Games.Generic;

public static class Services
{
    public static IServiceCollection AddGenericGameSupport(this IServiceCollection services)
    {
        services.AddSingleton<IniAnalysisData>();
        services.AddSingleton<GameToolRunner>();
        services.AddIniFileDefinitionModel();
        services.AddIniFileEntryModel();
        return services;
    }
}
