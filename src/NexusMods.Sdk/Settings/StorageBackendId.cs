using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Sdk.Settings;

/// <summary>
/// Represents a unique identifier for a storage backend.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct StorageBackendId : IAugmentWith<DefaultValueAugment>
{
    /// <inheritdoc/>
    public static StorageBackendId DefaultValue { get; } = From(Guid.Empty);
}

[PublicAPI]
public static class StorageBackends
{
    public static readonly StorageBackendId Json = StorageBackendId.From(Guid.Parse("ef1470a8-871a-440a-8352-b8d930b776de"));
}
