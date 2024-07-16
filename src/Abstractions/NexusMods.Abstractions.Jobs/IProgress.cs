using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public interface IProgress
{
    /// <summary>
    /// Gets the date when the job was first started.
    /// </summary>
    /// <remarks>
    /// This value is unavailable if the job has never been started.
    /// </remarks>
    Optional<DateTime> FirstStartTime { get; }

    /// <summary>
    /// Gets the date when the job was last started.
    /// </summary>
    /// <remarks>
    /// This value is unavailable if the job has never been started.
    /// </remarks>
    Optional<DateTime> LastStartTime { get; }

    /// <summary>
    /// Gets the date when the job was finished.
    /// </summary>
    /// <remarks>
    /// This value is unavailable if the job hasn't finished yet.
    /// </remarks>
    Optional<DateTime> FinishTime { get; }

    /// <summary>
    /// Gets the total duration of how long the job was running for.
    /// </summary>
    /// <remarks>
    /// This value is monotonically increasing. The value will stay constant,
    /// if the job is paused and will continue to increase after it has been
    /// resumed again.
    /// </remarks>
    TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets the observable stream for changes to <see cref="TotalDuration"/>.
    /// </summary>
    IObservable<TimeSpan> ObservableTotalDuration { get; }
}
