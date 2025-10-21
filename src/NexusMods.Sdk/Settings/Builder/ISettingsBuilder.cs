using System.Linq.Expressions;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public interface ISettingsBuilder
{
    /// <summary>
    /// Configure the default value to use a factory instead of new().
    /// </summary>
    /// <remarks>
    /// The type still requires a default constructor, even if it's not called.
    /// </remarks>
    ISettingsBuilder ConfigureDefault<TSettings>(
        Func<IServiceProvider, TSettings> defaultValueFactory
    ) where TSettings : class, ISettings, new();

    /// <summary>
    /// Configure the storage backend to use for this settings type.
    /// </summary>
    ISettingsBuilder ConfigureBackend(StorageBackendOptions options);

    /// <summary>
    /// Configure a property.
    /// </summary>
    ISettingsBuilder ConfigureProperty<TSettings, TProperty>(
        Expression<Func<TSettings, TProperty>> propertySelector,
        PropertyOptions<TSettings, TProperty> options,
        IContainerOptions? containerOptions
    )
        where TSettings : class, ISettings, new()
        where TProperty : notnull;
}
