using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Sdk.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class ServiceCollectionExtensions
{
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
