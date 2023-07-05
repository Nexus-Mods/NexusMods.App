using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A job that adds a mod to a loadout
/// </summary>
[JsonName(nameof(AddModJob))]
public record AddModJob : AJobEntity, IModJob
{
    /// <inheritdoc />
    public LoadoutId LoadoutId { get; init; }

    /// <inheritdoc />
    public ModId ModId { get; init; }
}
