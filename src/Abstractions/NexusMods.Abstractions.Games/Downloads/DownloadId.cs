using TransparentValueObjects;

namespace NexusMods.Abstractions.Games.Downloads;

/// <summary>
/// Id for a registered download
/// </summary>
[ValueObject<Guid>]
public readonly partial struct DownloadId : IAugmentWith<JsonAugment> { }
