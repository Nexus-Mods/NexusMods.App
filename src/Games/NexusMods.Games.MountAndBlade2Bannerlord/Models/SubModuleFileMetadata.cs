using Bannerlord.ModuleManager;
using JetBrains.Annotations;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.Models.SubModuleFileMetadata")]
public class SubModuleFileMetadata : IMetadata
{
    public bool IsValid { get; set; } // TODO: I guess this is where we will store the validation check result?
    public required ModuleInfoExtended ModuleInfo { get; init; }
}
