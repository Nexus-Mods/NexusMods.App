using NexusMods.MnemonicDB.Abstractions;
using TransparentValueObjects;

namespace NexusMods.Abstractions.FileStore.Downloads;

/// <summary>
/// Id for a registered download
/// </summary>
[ValueObject<EntityId>]
[Obsolete(message: "To be replaced with Library Items and Jobs")]
public readonly partial struct DownloadId : ITypedEntityId
{
    EntityId ITypedEntityId.Value => Value;
}
