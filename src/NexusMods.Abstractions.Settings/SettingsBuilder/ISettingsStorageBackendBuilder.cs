using JetBrains.Annotations;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Configuration builder for <typeparamref name="TSettings"/> for the storage backend.
/// </summary>
[PublicAPI]
public interface ISettingsStorageBackendBuilder<TSettings> where TSettings : class, ISettings, new()
{
    /// <summary>
    /// Don't assign this settings to any storage backend. This disables
    /// the storing and loading of values for this type.
    /// </summary>
    void Disable();

    /// <summary>
    /// Use the storage backend with the provided ID.
    /// </summary>
    void UseStorageBackend(SettingsStorageBackendId id);

    /// <summary>
    /// Use the provided storage backend.
    /// </summary>
    void UseStorageBackend<TBackend>() where TBackend : IBaseSettingsStorageBackend;
}
