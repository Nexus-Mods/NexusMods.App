using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A job that shows progress of the game management tasks
/// </summary>
[JsonName(nameof(ManageGameJob))]
public record ManageGameJob : AJobEntity, ILoadoutJob
{
    /// <inheritdoc />
    public LoadoutId LoadoutId { get; init; }
}
