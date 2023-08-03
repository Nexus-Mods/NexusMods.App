using Bannerlord.ModuleManager;
using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.ModuleInfoMetadata")]
public class ModuleInfoMetadata : IMetadata
{
    public required ModuleInfoExtended ModuleInfo { get; init; } = new();
}