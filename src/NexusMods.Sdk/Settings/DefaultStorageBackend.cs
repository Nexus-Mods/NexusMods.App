using JetBrains.Annotations;

namespace NexusMods.Sdk.Settings;

[PublicAPI]
public record DefaultStorageBackend(IBaseStorageBackend Backend);
