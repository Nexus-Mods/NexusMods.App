using TransparentValueObjects;

namespace NexusMods.DataModel;

/// <summary>
/// Id for a registered download
/// </summary>
[ValueObject<Guid>]
public readonly partial struct DownloadId : IAugmentWith<JsonAugment> { }
