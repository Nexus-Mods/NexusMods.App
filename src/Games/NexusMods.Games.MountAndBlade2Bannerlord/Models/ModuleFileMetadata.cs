using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.ModuleFileMetadata")]
public class ModuleFileMetadata : IMetadata
{
    public required string OriginalRelativePath { get; init; }
}
