using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.MnemonicDB.Analyzers;

public static class Services
{
    public static IServiceCollection AddAnalyzers(this IServiceCollection services)
    {
        return services.AddSingleton<ITreeAnalyzer, TreeAnalyzer>();
    }
}
