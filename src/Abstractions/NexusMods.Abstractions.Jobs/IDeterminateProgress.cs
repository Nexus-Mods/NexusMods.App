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
    /// Gets the current rate at which work is being done.
    /// </summary>
    ProgressRate ProgressRate { get; }

    /// <summary>
    /// Gets the estimated finish time.
    /// </summary>
    /// <remarks>
    /// This value is unavailable if the job isn't running.
    /// </remarks>
    Optional<DateTime> EstimatedFinishTime { get; }

    /// <summary>
    /// Gets the observable stream for changes to <see cref="Percent"/>.
    /// </summary>
    IObservable<Percent> ObservablePercent { get; }

    /// <summary>
    /// Gets the observable stream for changes to <see cref="ProgressRate"/>.
    /// </summary>
    IObservable<ProgressRate> ObservableProgressRate { get; }

    /// <summary>
    /// Gets the observable stream for changes to <see cref="EstimatedFinishTime"/>.
    /// </summary>
    IObservable<DateTime> ObservableEstimatedFinishTime { get; }
}
