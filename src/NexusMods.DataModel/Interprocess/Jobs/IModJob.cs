using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

/// <summary>
/// A job that references a mod
/// </summary>
public interface IModJob : ILoadoutJob
{
    /// <summary>
    /// The mod id this job is for
    /// </summary>
    public ModId ModId { get; }
}
