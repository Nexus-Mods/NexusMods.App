using Microsoft.Extensions.DependencyInjection;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.MnemonicDB.Analyzers;

public static class Services
{
    public static IServiceCollection AddAnalyzers(this IServiceCollection services)
    {
        return services.AddSingleton<IAnalyzer, TreeAnalyzer>();
    }
}
