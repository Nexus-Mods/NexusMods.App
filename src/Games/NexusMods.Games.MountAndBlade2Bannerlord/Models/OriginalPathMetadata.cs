using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.OriginalPathMetadata")]
public class OriginalPathMetadata : IMetadata
{
    public required string OriginalRelativePath { get; init; } = string.Empty;
}
