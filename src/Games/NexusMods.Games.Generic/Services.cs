using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.Generic.Entities;
using NexusMods.Games.Generic.FileAnalyzers;
using NexusMods.Games.Generic.Installers;

namespace NexusMods.Games.Generic;

public static class Services
{
    public static IServiceCollection AddGenericGameSupport(this IServiceCollection services)
    {
        services.AddSingleton<IFileAnalyzer, IniAnalzyer>();
        services.AddSingleton<IniAnalysisData>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}
