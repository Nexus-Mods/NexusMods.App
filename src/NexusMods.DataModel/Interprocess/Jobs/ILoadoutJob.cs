using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A interprocess job that references a loadout
/// </summary>
public interface ILoadoutJob
{
    /// <summary>
    /// The loadout id this job is for
    /// </summary>
    public LoadoutId LoadoutId { get; }
}
