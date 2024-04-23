using NexusMods.MnemonicDB.Abstractions;
using TransparentValueObjects;

namespace NexusMods.Abstractions.FileStore.Downloads;

/// <summary>
/// Id for a registered download
/// </summary>
[ValueObject<EntityId>]
public readonly partial struct DownloadId : ITypedEntityId
{
    EntityId ITypedEntityId.Value => Value;
}
