using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IJobGroup"/>.
/// </summary>
[PublicAPI]
public interface IMutableJobGroup : IControllableJobGroup, IMutableJob
{
    /// <summary>
    /// Adds a job to the group.
    /// </summary>
    void AddJob(IControllableJob job);
}
