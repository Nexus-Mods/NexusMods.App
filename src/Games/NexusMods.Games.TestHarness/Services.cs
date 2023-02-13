using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.Games.TestHarness.Verbs;

namespace NexusMods.Games.TestHarness;

public static class Services
{
    public static IServiceCollection AddTestHarness(this IServiceCollection services)
    {
        services.AddVerb<StressTest>();
        services.AddSingleton<ITypeFinder, TypeFinder>();
        return services;
    }
}