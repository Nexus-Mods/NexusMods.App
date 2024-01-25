using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Extensions.DependencyInjection;

/// <summary>
/// Extensions for DI implementations based on Microsoft's Dependency Injection container.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TService"/> and <typeparamref name="TImplementation"/> as scoped services.
    /// </summary>
    /// <param name="services">Collection to register to.</param>
    /// <param name="ctor">Constructor for the base item.</param>
    /// <typeparam name="TService">The base service/interface under which to register the item.</typeparam>
    /// <typeparam name="TImplementation">The specific implementation of the item.</typeparam>
    /// <returns>The new service collection.</returns>
    public static IServiceCollection AddAllScoped<TService, TImplementation>(this IServiceCollection services,
        Func<IServiceProvider, TImplementation>? ctor = null)
        where TImplementation : class, TService
        where TService : class
    {
        if (ctor == null)
            services.AddScoped<TImplementation>();
        else
            services.AddScoped(ctor);

        services.AddScoped<TService, TImplementation>(s => s.GetService<TImplementation>()!);
        return services;
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
        if (ctor == null)
            services.AddSingleton<TImplementation>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<TService, TImplementation>(s => s.GetService<TImplementation>()!);
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TService"/> and <typeparamref name="TImplementation"/> as singletons,
    /// but in a way where <typeparamref name="TService"/> and <typeparamref name="TImplementation"/> share the same instance.
    /// </summary>
    /// <param name="services">Collection to register to.</param>
    /// <param name="ctor">Constructor for the base item.</param>
    /// <typeparam name="TImplementation">The specific implementation of the item.</typeparam>
    /// <typeparam name="TService1"></typeparam>
    /// <typeparam name="TService2"></typeparam>
    /// <returns>The new service collection.</returns>
    public static IServiceCollection AddAllSingleton<TService1, TService2, TImplementation>(this IServiceCollection services,
        Func<IServiceProvider, TImplementation>? ctor = null)
        where TImplementation : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        if (ctor == null)
            services.AddSingleton<TImplementation>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<TService1, TImplementation>(s => s.GetService<TImplementation>()!);
        services.AddSingleton<TService2, TImplementation>(s => s.GetService<TImplementation>()!);
        return services;
    }
}
