using Bannerlord.ModuleManager;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Loadouts;

[JsonName("MountAndBlade2Bannerlord.Loadouts.ModuleInfoMetadata")]
public class ModuleInfoMetadata : IModFileMetadata
{
    public required ModuleInfoExtended ModuleInfo { get; init; } = new();
}