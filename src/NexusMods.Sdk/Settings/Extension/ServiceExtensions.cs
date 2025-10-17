using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public static class ServiceExtensions
{
    public static IServiceCollection AddSettings<T>(this IServiceCollection serviceCollection)
        where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new SettingsRegistration(
            ObjectType: typeof(T),
            DefaultValue: new T(),
            ConfigureLambda: T.Configure
        ));
    }

    public static IServiceCollection AddStorageBackend<T>(
        this IServiceCollection serviceCollection,
        bool isDefault = false)
        where T : class, IBaseStorageBackend
    {
        serviceCollection = serviceCollection.AddSingleton<IBaseStorageBackend, T>();
        if (!isDefault) return serviceCollection;

        return serviceCollection
            .AddSingleton<T>()
            .AddSingleton<DefaultStorageBackend>(serviceProvider => new DefaultStorageBackend(serviceProvider.GetRequiredService<T>())
        );
    }

    public static IServiceCollection OverrideSettingsForTests<T>(
        this IServiceCollection serviceCollection,
        Func<T, T> overrideMethod
    ) where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new OverrideHack(typeof(T), Hack));
        object Hack(object obj)
        {
            if (obj is not T value) throw new ArgumentException($"Expected value of type {typeof(T)}", nameof(obj));
            return overrideMethod(value);
        }
    }
}
