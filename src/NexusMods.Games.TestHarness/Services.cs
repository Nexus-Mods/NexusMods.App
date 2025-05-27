using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.TestHarness.Verbs;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.Games.TestHarness;

public static class Services
{
    public static IServiceCollection AddTestHarness(this IServiceCollection services)
    {
        services.AddVerb(() => StressTest.RunStressTest);
        return services;
    }
}
