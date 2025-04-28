using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Configuration builder for types that implement <see cref="ISettings"/>.
/// </summary>
[PublicAPI]
public interface ISettingsBuilder
{
    /// <summary>
    /// Configures the settings type <typeparamref name="TSettings"/> to be
    /// exposed in the UI.
    /// </summary>
    ISettingsBuilder AddToUI<TSettings>(
        Func<ISettingsUIBuilder<TSettings>, ISettingsUIBuilder<TSettings>> configureUI
    ) where TSettings : class, ISettings, new();

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
    /// Configures the storage backend for this setting.
    /// </summary>
    ISettingsBuilder ConfigureStorageBackend<TSettings>(
        Action<ISettingsStorageBackendBuilder<TSettings>> configureStorageBackend
    ) where TSettings : class, ISettings, new();
}
