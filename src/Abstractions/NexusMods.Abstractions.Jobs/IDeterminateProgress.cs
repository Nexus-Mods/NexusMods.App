using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Determinate progress indicates a specific and measurable portion of a job
/// that has been completed.
/// </summary>
[PublicAPI]
public interface IDeterminateProgress : IProgress
{
    /// <summary>
    /// Gets the current amount of work done as a percentage.
    /// </summary>
    Percent Percent { get; }

    /// <summary>
    /// Gets the estimated finish time.
    /// </summary>
    /// <remarks>
    /// This value is unavailable if the job isn't running.
    /// </remarks>
    Optional<DateTime> EstimatedFinishTime { get; }
}
