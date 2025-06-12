using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NexusMods.Sdk;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Validates that the given <see cref="IServiceCollection"/> contains no duplicate service registrations.
    /// </summary>
    public static IServiceCollection Validate(this IServiceCollection serviceCollection)
    {
        if (!ApplicationConstants.IsDebug) return serviceCollection;

        var serviceDescriptors = serviceCollection
            .Where(sd => sd is { IsKeyedService: false, ImplementationType: not null })
            .Where(sd => sd.ServiceType != typeof(IStartupValidator))
            .GroupBy(sd => (sd.ServiceType, sd.ImplementationType))
            .Where(g => g.Count() > 1)
            .Select(g => (g.Key.ServiceType, g.Key.ImplementationType, g.Count()))
            .ToList();

        if (serviceDescriptors.Count == 0) return serviceCollection;

        var sb = new StringBuilder();
        foreach (var error in serviceDescriptors)
        {
            sb.AppendLine($"  Service: {error.ServiceType}, Implementation: {error.ImplementationType}, Count: {error.Item3}");
        }

        throw new InvalidOperationException($"Duplicate service registrations found: \n{sb}");
    }

    /// <summary>
    /// Registers <typeparamref name="TService"/> and <typeparamref name="TImplementation"/> as singletons,
    /// but in a way where <typeparamref name="TService"/> and <typeparamref name="TImplementation"/> share the same instance.
    /// </summary>
    /// <param name="services">Collection to register to.</param>
    /// <param name="ctor">Constructor for the base item.</param>
    /// <typeparam name="TService">The base service/interface under which to register the item.</typeparam>
    /// <typeparam name="TImplementation">The specific implementation of the item.</typeparam>
    /// <returns>The new service collection.</returns>
    public static IServiceCollection AddAllSingleton<TService, TImplementation>(this IServiceCollection services,
        Func<IServiceProvider, TImplementation>? ctor = null)
        where TImplementation : class, TService
        where TService : class
    {
        if (ctor is null) services.AddSingleton<TImplementation>();
        else services.AddSingleton(ctor);

        services.AddSingleton<TService, TImplementation>(s => s.GetRequiredService<TImplementation>());
        return services;
    }
}
