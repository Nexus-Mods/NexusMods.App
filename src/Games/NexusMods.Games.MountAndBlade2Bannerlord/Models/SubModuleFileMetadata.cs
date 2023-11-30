using Bannerlord.ModuleManager;
using JetBrains.Annotations;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Models;

[PublicAPI]
[JsonName("NexusMods.Games.MountAndBlade2Bannerlord.SubModuleFileMetadata")]
public class SubModuleFileMetadata : IMetadata
{
    public bool IsValid { get; set; } // TODO: I guess this is where we will store the validation check result?
    public required ModuleInfoExtended ModuleInfo { get; init; }
}
