using System.Collections.ObjectModel;
using DynamicData;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a monitor for jobs.
/// </summary>
[PublicAPI]
public interface IJobMonitor
{
    /// <summary>
    /// Gets an observable collection containing every job the monitor knows about.
    /// </summary>
    ReadOnlyObservableCollection<IJob> Jobs { get; }

    /// <summary>
    /// Gets an observable with changeset for jobs of type <typeparamref name="TJob"/>.
    /// </summary>
    IObservable<IChangeSet<TJob, JobId>> GetObservableChangeSet<TJob>() where TJob : IJob;

    /// <summary>
    /// Registers a job with the monitor.
    /// </summary>
    void RegisterJob(IJob job);
}
