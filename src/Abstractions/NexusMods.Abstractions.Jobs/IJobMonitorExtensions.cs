using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;


namespace NexusMods.Abstractions.Jobs;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Extensions for <see cref="IJobMonitor"/>
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
    public static IObservable<IChangeSet<IJob, JobId>> ObserveActiveJobs<TJobType>(this IJobMonitor jobMonitor) 
        where TJobType : IJobDefinition
    {
        return jobMonitor.GetObservableChangeSet<TJobType>()
            .FilterOnObservable(job => job.ObservableStatus.Select(status => status.IsActive()));
    }

    public static IObservable<bool> HasActiveJob<TJobType>(this IJobMonitor jobMonitor, Func<TJobType, bool> predicate)
        where TJobType : IJobDefinition
    {
        return jobMonitor
            .ObserveActiveJobs<TJobType>()
            .QueryWhenChanged(query => query.Items.Any(x =>
            {
                if (x.Definition is not TJobType concrete) return false;
                return predicate(concrete);
            }));
    }
    
    /// <summary>
    /// Gets an observable of the average progress percent of all given jobs.
    /// </summary>
    public static IObservable<Percent> AverageProgressPercent<TJobType>(this IObservable<IChangeSet<TJobType, JobId>> jobs) 
        where TJobType : IJob
    {
        return jobs.TransformOnObservable(job => job.ObservableProgress)
            .Filter(p => p.HasValue)
            .QueryWhenChanged(coll =>
                {
                    if (coll.Count == 0)
                        return Percent.Zero;
                    return Percent.CreateClamped(coll.Items.Aggregate(0.0d, (acc, source) => acc + source.Value.Value) / coll.Count);
                }
            );
    }
    
    /// <summary>
    /// Gets an observable of the sum of the progress rate of all given jobs.
    /// </summary>
    public static IObservable<double> SumRateOfProgress<TJobType>(this IObservable<IChangeSet<TJobType, JobId>> jobs) 
        where TJobType : IJob
    {
        return jobs.TransformOnObservable(job => job.ObservableRateOfProgress)
            .Filter(p => p.HasValue)
            .QueryWhenChanged(coll =>
                {
                    if (coll.Count == 0)
                        return 0.0d;
                    return coll.Items.Where(f => f.HasValue)
                        .Select(r => r.Value).Sum();
                }
            );
    }
}
