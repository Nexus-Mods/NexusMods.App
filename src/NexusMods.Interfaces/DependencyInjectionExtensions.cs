using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Interfaces;

public static class DependencyInjectionExtensions
{
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