using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// </summary>
[PublicAPI]
public static class ServiceExtensions
{
    /// <summary>
    /// Registers a settings type.
    /// </summary>
    public static IServiceCollection AddSettings<T>(this IServiceCollection serviceCollection)
        where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new SettingsTypeInformation(
            ObjectType: typeof(T),
            DefaultValue: new T(),
            ConfigureLambda: T.Configure
        ));
    }

    /// <summary>
    /// Registers a new settings section.
    /// </summary>
    public static IServiceCollection AddSettingsSection(this IServiceCollection serviceCollection, SettingsSectionSetup setup)
    {
        return serviceCollection.AddSingleton(setup);
    }

    /// <summary>
    /// Registers a settings storage backend in DI.
    /// </summary>
    /// <param name="serviceCollection">The Service Collection.</param>
    /// <param name="isDefault">
    /// Whether the backend should be registered as the default backend.
    /// </param>
    public static IServiceCollection AddSettingsStorageBackend<T>(
        this IServiceCollection serviceCollection,
        bool isDefault = false)
        where T : class, IBaseSettingsStorageBackend
    {
        serviceCollection = serviceCollection.AddSingleton<IBaseSettingsStorageBackend, T>();
        if (!isDefault) return serviceCollection;
        return serviceCollection
            .AddSingleton<T>()
            .AddSingleton<DefaultSettingsStorageBackend>(serviceProvider => new DefaultSettingsStorageBackend(serviceProvider.GetRequiredService<T>())
        );
    }

    /// <summary>
    /// Registers an override for <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// This method only exists for tests. As an example, SQLite has an in-memory
    /// mode that should be used for tests. You can create a new settings type for
    /// SQLite that has a UseInMemory property. This property will be set to false
    /// by default when using the program normally. For tests, we want to override
    /// this property.
    ///
    /// <see cref="OverrideSettingsForTests{T}"/> does exactly that. It allows you
    /// to override the value for a settings type. Everytime the current value of
    /// <typeparamref name="T"/> is fetched, the lambda <paramref name="overrideMethod"/>
    /// will be invoked. This allows you to override any value and configure settings
    /// for tests.
    /// </remarks>
    public static IServiceCollection OverrideSettingsForTests<T>(
        this IServiceCollection serviceCollection,
        Func<T, T> overrideMethod)
        where T : class, ISettings, new()
    {
        return serviceCollection.AddSingleton(new SettingsOverrideInformation(typeof(T), Hack));

        object Hack(object obj) => overrideMethod((T)obj);
    }

    /// <summary>
    /// Use the JSON storage backend for this setting.
    /// </summary>
    public static void UseJson<T>(this ISettingsStorageBackendBuilder<T> builder)
        where T : class, ISettings, new()
    {
        builder.UseStorageBackend(JsonStorageBackend.StaticId);
    }
}
