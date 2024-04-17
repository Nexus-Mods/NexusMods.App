using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents a unique identifier for a storage backend.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct SettingsStorageBackendId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static SettingsStorageBackendId DefaultValue { get; } = From(Guid.Empty);
}
