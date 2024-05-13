using System;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.SingleProcess;

/// <summary>
/// Extension methods for the service collection.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add the single process services to the service collection.
    /// </summary>
    /// <param name="services"></param>s
    /// <returns></returns>
    public static IServiceCollection AddSingleProcess(this IServiceCollection services) =>
        services.AddSingleton<MainProcessDirector>()
            .AddSingleton<ClientProcessDirector>()
            .AddSingleton<StartupDirector>();
}
