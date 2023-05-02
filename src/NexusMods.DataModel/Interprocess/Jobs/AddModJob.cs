using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

[JsonName(nameof(AddModJob))]
public record AddModJob : AJobEntity, IModJob
{
    public LoadoutId LoadoutId { get; init; }
    public ModId ModId { get; init; }
}
