using JetBrains.Annotations;
using NexusMods.Abstractions.Settings;

namespace NexusMods.Abstractions.Serialization.Settings;

/// <summary>
/// Extension methods for settings.
/// </summary>
[PublicAPI]
public static class Extensions
{
    internal static SettingsStorageBackendId StorageBackendId = SettingsStorageBackendId.From(Guid.Parse("6317967a-3ee9-4e3f-bb24-ebbe40560160"));

    /// <summary>
    /// Use the data store as a storage backend for this setting.
    /// </summary>
    public static void UseDataStore<T>(this ISettingsStorageBackendBuilder<T> builder)
        where T : class, ISettings, new()
    {
        builder.UseStorageBackend(StorageBackendId);
    }
}
