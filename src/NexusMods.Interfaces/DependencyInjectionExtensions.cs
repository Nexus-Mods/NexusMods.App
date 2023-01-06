using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Interfaces;

public static class DependencyInjectionExtensions
{
    
    /// <summary>
    /// Registers TBase and T1 as scoped services.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ctor"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TBase"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAllScoped<T1, TBase>(this IServiceCollection services,
        Func<IServiceProvider, TBase>? ctor = null)
        where TBase : class, T1
        where T1 : class
    {
        if (ctor == null)
            services.AddScoped<TBase>();
        else
            services.AddScoped(ctor);

        services.AddScoped<T1, TBase>(s => s.GetService<TBase>()!);
        return services;
    }

    /// <summary>
    /// Registers TBase and T1 as singletons, but in a way where T1 and TBase share the same instance.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ctor"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TBase"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAllSingleton<T1, TBase>(this IServiceCollection services,
        Func<IServiceProvider, TBase>? ctor = null)
        where TBase : class, T1
        where T1 : class
    {
        if (ctor == null)
            services.AddSingleton<TBase>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
        return services;
    }

    /// <summary>
    /// Registers T1, T2, and TBase as singletons, but in a way
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ctor"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TBase"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAllSingleton<T1, T2, TBase>(this IServiceCollection services,
        Func<IServiceProvider, TBase>? ctor = null)
        where TBase : class, T1, T2
        where T1 : class
        where T2 : class
    {
        if (ctor == null)
            services.AddSingleton<TBase>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
        return services;
    }

    /// <summary>
    /// Registers T1, T2, T3, and TBase as singletons, but in a way where 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ctor"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="TBase"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAllSingleton<T1, T2, T3, TBase>(this IServiceCollection services,
        Func<IServiceProvider, TBase>? ctor = null)
        where TBase : class, T1, T2, T3
        where T1 : class
        where T2 : class
        where T3 : class
    {
        if (ctor == null)
            services.AddSingleton<TBase>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T3, TBase>(s => s.GetService<TBase>()!);
        return services;
    }

    /// <summary>
    /// Registers T1, T2, T3, T4, and TBase as singletons, but in a way where T1
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ctor"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="TBase"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddAllSingleton<T1, T2, T3, T4, TBase>(this IServiceCollection services,
        Func<IServiceProvider, TBase>? ctor = null)
        where TBase : class, T1, T2, T3, T4
        where T1 : class
        where T2 : class
        where T3 : class
        where T4 : class
    {
        if (ctor == null)
            services.AddSingleton<TBase>();
        else
            services.AddSingleton(ctor);

        services.AddSingleton<T1, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T2, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T3, TBase>(s => s.GetService<TBase>()!);
        services.AddSingleton<T4, TBase>(s => s.GetService<TBase>()!);
        return services;
    }
}