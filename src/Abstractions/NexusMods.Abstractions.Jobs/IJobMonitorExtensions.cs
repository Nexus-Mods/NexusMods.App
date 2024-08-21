using System.Reactive.Linq;
using DynamicData;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Extensions for <see cref="IJobMonitor"/>.
/// </summary>
public static class IJobMonitorExtensions
{
    /// <summary>
    /// Returns an observable collection containing every job of the given type the monitor knows about, filtered to only include active (Paused or Running) jobs.
    ///
    /// Note: as job status changes, the changeset will be updated to reflect the new status.
    /// </summary>
    /// <param name="jobMonitor"></param>
    /// <typeparam name="TJobType"></typeparam>
    /// <returns></returns>
    public static IObservable<IChangeSet<TJobType, JobId>> ObserveActiveJobs<TJobType>(this IJobMonitor jobMonitor) 
        where TJobType : IJob
    {
        return jobMonitor.GetObservableChangeSet<TJobType>()
            .FilterOnObservable(job => job.ObservableStatus
                .Select(status => status is JobStatus.Running or JobStatus.Paused));
    } 
    
    
    /// <summary>
    /// Gets an observable of the average progress percent of all given jobs.
    /// </summary>
    public static IObservable<Percent> AverageProgressPercent<TJobType>(this IObservable<IChangeSet<TJobType, JobId>> jobs) 
        where TJobType : IJob
    {
        return jobs.TransformOnObservable(job =>
                {
                    if (!job.Progress.TryGetDeterminateProgress(out var determinateProgress))
                        return Observable.Empty<Percent>();
                    return determinateProgress.ObservablePercent;
                }
            )
            .QueryWhenChanged(coll =>
                {
                    if (coll.Count == 0)
                        return Percent.Zero;
                    return Percent.CreateClamped(coll.Items.Aggregate(0.0d, (acc, source) => acc + source.Value) / coll.Count);
                }
            );
    }
    
    /// <summary>
    /// Gets an observable of the sum of the progress rate of all given jobs.
    /// </summary>
    public static IObservable<double> SumProgressRate<TJobType>(this IObservable<IChangeSet<TJobType, JobId>> jobs) 
        where TJobType : IJob
    {
        return jobs.TransformOnObservable(job =>
                {
                    if (!job.Progress.TryGetDeterminateProgress(out var determinateProgress))
                        return Observable.Empty<ProgressRate>();
                    return determinateProgress.ObservableProgressRate;
                }
            )
            .QueryWhenChanged(coll =>
                {
                    if (coll.Count == 0)
                        return 0.0d;
                    return coll.Items.Select(r => r.Value).Sum();
                }
            );
    }
}
