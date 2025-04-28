using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Games.TestHarness.Verbs;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.Games.TestHarness;

public static class Services
{
    public static IServiceCollection AddTestHarness(this IServiceCollection services)
    {
        services.AddVerb(() => StressTest.RunStressTest);
        return services;
    }
}
