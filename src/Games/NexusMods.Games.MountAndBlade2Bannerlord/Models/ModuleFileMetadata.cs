using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.ModuleFileMetadata")]
public class ModuleFileMetadata : IMetadata
{
    public required string OriginalRelativePath { get; init; }
}
