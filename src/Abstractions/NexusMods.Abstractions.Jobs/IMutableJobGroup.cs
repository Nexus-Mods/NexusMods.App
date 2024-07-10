using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IJobGroup"/>.
/// </summary>
[PublicAPI]
public interface IMutableJobGroup : IJobGroup
{
    /// <summary>
    /// Adds a job to the group.
    /// </summary>
    void AddJob(IControllableJob job);
}
