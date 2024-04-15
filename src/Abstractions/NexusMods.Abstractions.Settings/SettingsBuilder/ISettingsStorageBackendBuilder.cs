using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Configuration builder for <typeparamref name="TSettings"/> for the storage backend.
/// </summary>
[PublicAPI]
public interface ISettingsStorageBackendBuilder<TSettings> where TSettings : class, ISettings, new()
{
    /// <summary>
    /// Use the storage backend with the provided ID.
    /// </summary>
    ISettingsStorageBackendBuilder<TSettings> UseStorageBackend(SettingsStorageBackendId id);

    /// <summary>
    /// Use the provided storage backend.
    /// </summary>
    ISettingsStorageBackendBuilder<TSettings> UseStorageBackend<TBackend>() where TBackend : IBaseSettingsStorageBackend;
}
