using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

[JsonName(nameof(ManageGameJob))]
public record ManageGameJob : AJobEntity, ILoadoutJob
{
    public LoadoutId LoadoutId { get; init; }
}
