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
    /// Registers an override for <typeparamref name="T"/>.
    /// </summary>
    public static IServiceCollection OverrideSettings<T>(
        this IServiceCollection serviceCollection,
        Func<T, T> overrideMethod)
        where T : class, ISettings, new()
    {
        var hack = (Func<object, object>)overrideMethod;
        return serviceCollection.AddSingleton(new SettingsOverrideInformation(typeof(T), hack));
    }
}
